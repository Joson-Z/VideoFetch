using System.Globalization;
using System.Text.Json;
using VideoFetch.Application.Downloads;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;

namespace VideoFetch.Infrastructure.Ffmpeg;

public sealed class FfprobeMediaValidationService(
    ToolchainConfiguration configuration,
    IExecutableLocator executableLocator,
    IProcessRunner processRunner) : IMediaValidationService
{
    private static readonly TimeSpan ValidationTimeout = TimeSpan.FromSeconds(20);

    public async Task<MediaValidationResult> ValidateAsync(
        string filePath,
        TimeSpan? expectedDuration = null,
        bool expectAudio = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath) || new FileInfo(fullPath).Length == 0)
        {
            return Invalid(fullPath, "输出文件不存在或为空。");
        }

        string? executable = executableLocator.Find(
            "ffprobe.exe",
            configuration.FfprobePath,
            configuration.ToolDirectory);
        if (executable is null)
        {
            return Invalid(fullPath, "未找到 ffprobe.exe，无法校验输出文件。");
        }

        ProcessResult result = await processRunner.RunAsync(
            new ProcessRequest
            {
                FileName = executable,
                Arguments = ["-v", "error", "-print_format", "json", "-show_format", "-show_streams", "--", fullPath],
                Timeout = ValidationTimeout,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Invalid(fullPath, "ffprobe 无法读取输出文件。");
        }

        try
        {
            return ParseProbeResult(fullPath, result.StandardOutput, expectedDuration, expectAudio);
        }
        catch (JsonException)
        {
            return Invalid(fullPath, "ffprobe 返回了无法解析的校验结果。");
        }
    }

    private static MediaValidationResult ParseProbeResult(
        string fullPath,
        string json,
        TimeSpan? expectedDuration,
        bool expectAudio)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        string? formatName = TryGetString(root, "format", "format_name");
        bool isMp4 = formatName?.Split(',').Any(value =>
            value.Equals("mp4", StringComparison.OrdinalIgnoreCase)
            || value.Equals("mov", StringComparison.OrdinalIgnoreCase)) == true;

        string? videoCodec = null;
        string? audioCodec = null;
        if (root.TryGetProperty("streams", out JsonElement streams) && streams.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement stream in streams.EnumerateArray())
            {
                string? codecType = TryGetString(stream, "codec_type");
                string? codecName = TryGetString(stream, "codec_name");
                if (videoCodec is null && codecType == "video")
                {
                    videoCodec = codecName;
                }
                else if (audioCodec is null && codecType == "audio")
                {
                    audioCodec = codecName;
                }
            }
        }

        TimeSpan? duration = ParseDuration(TryGetString(root, "format", "duration"));
        List<string> issues = [];
        if (!isMp4)
        {
            issues.Add("输出容器不是 MP4。");
        }

        if (videoCodec is null)
        {
            issues.Add("输出文件缺少视频流。");
        }

        if (expectAudio && audioCodec is null)
        {
            issues.Add("输出文件缺少音频流。");
        }

        if (expectedDuration is { } expected && duration is { } actual)
        {
            double toleranceSeconds = Math.Max(3, expected.TotalSeconds * 0.02);
            if (Math.Abs((actual - expected).TotalSeconds) > toleranceSeconds)
            {
                issues.Add("输出时长与视频信息不一致。");
            }
        }

        return new MediaValidationResult
        {
            FilePath = fullPath,
            IsReadable = true,
            IsMp4 = isMp4,
            HasVideo = videoCodec is not null,
            HasAudio = audioCodec is not null,
            VideoCodec = videoCodec,
            AudioCodec = audioCodec,
            Duration = duration,
            Issues = issues,
        };
    }

    private static TimeSpan? ParseDuration(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;

    private static string? TryGetString(JsonElement element, params string[] path)
    {
        JsonElement current = element;
        foreach (string propertyName in path)
        {
            if (!current.TryGetProperty(propertyName, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static MediaValidationResult Invalid(string path, string issue) =>
        new()
        {
            FilePath = path,
            Issues = [issue],
        };
}
