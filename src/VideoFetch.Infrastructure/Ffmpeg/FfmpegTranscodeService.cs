using System.Globalization;
using VideoFetch.Application.Downloads;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Domain.Downloads;
using VideoFetch.Infrastructure.Processes;

namespace VideoFetch.Infrastructure.Ffmpeg;

public sealed class FfmpegTranscodeService(
    ToolchainConfiguration configuration,
    IExecutableLocator executableLocator,
    IProcessRunner processRunner) : IMediaTranscodeService
{
    public async Task TranscodeToUniversalMp4Async(
        string inputPath,
        TimeSpan? duration,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        string fullInputPath = Path.GetFullPath(inputPath);
        if (!File.Exists(fullInputPath))
        {
            throw new DownloadException("待转码文件不存在。");
        }

        string? executable = executableLocator.Find(
            "ffmpeg.exe",
            configuration.FfmpegPath,
            configuration.ToolDirectory);
        if (executable is null)
        {
            throw new DownloadException("未找到 ffmpeg.exe，无法生成通用兼容 MP4。");
        }

        string directory = Path.GetDirectoryName(fullInputPath)!;
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileNameWithoutExtension(fullInputPath)}.{Guid.NewGuid():N}.transcoding.mp4");
        try
        {
            InlineProgress<ProcessOutputLine> outputProgress = new(line =>
                ReportProgress(line.Text, duration, progress));
            ProcessResult result = await processRunner.RunAsync(
                new ProcessRequest
                {
                    FileName = executable,
                    Arguments =
                    [
                        "-y",
                        "-i", fullInputPath,
                        "-map", "0:v:0",
                        "-map", "0:a:0?",
                        "-c:v", "libx264",
                        "-preset", "medium",
                        "-crf", "18",
                        "-c:a", "aac",
                        "-b:a", "192k",
                        "-movflags", "+faststart",
                        "-progress", "pipe:1",
                        "-nostats",
                        temporaryPath,
                    ],
                },
                outputProgress,
                cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess || !File.Exists(temporaryPath) || new FileInfo(temporaryPath).Length == 0)
            {
                throw new DownloadException("FFmpeg 转码失败，请查看脱敏日志了解详情。");
            }

            ReplaceOriginal(fullInputPath, temporaryPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void ReportProgress(
        string line,
        TimeSpan? duration,
        IProgress<DownloadProgress>? progress)
    {
        if (progress is null || duration is null || !line.StartsWith("out_time=", StringComparison.Ordinal))
        {
            return;
        }

        string value = line["out_time=".Length..].Trim();
        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan processed))
        {
            return;
        }

        double percent = duration.Value.TotalSeconds <= 0
            ? 0
            : Math.Clamp(processed.TotalSeconds / duration.Value.TotalSeconds * 100, 0, 100);
        progress.Report(new DownloadProgress(DownloadTaskState.Transcoding, percent, Message: "正在转码为 H.264/AAC MP4"));
    }

    private static void ReplaceOriginal(string originalPath, string temporaryPath)
    {
        string backupPath = $"{originalPath}.{Guid.NewGuid():N}.backup";
        File.Move(originalPath, backupPath);
        try
        {
            File.Move(temporaryPath, originalPath);
            File.Delete(backupPath);
        }
        catch
        {
            if (!File.Exists(originalPath) && File.Exists(backupPath))
            {
                File.Move(backupPath, originalPath);
            }

            throw;
        }
    }
}
