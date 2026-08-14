using VideoFetch.Application.Downloads;
using VideoFetch.Application.Files;
using VideoFetch.Application.Media;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Domain.Downloads;
using VideoFetch.Domain.Media;
using VideoFetch.Infrastructure.Processes;

namespace VideoFetch.Infrastructure.YtDlp;

public sealed class YtDlpDownloadService(
    YtDlpConfiguration configuration,
    IExecutableLocator executableLocator,
    IProcessRunner processRunner,
    IFileNameService fileNameService,
    IMediaValidationService validationService,
    IMediaTranscodeService transcodeService,
    YtDlpProgressParser progressParser) : IDownloadService
{
    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputDirectory);

        string? ytDlpPath = executableLocator.Find(
            "yt-dlp.exe",
            configuration.ExecutablePath,
            configuration.ToolDirectory);
        string? ffmpegPath = executableLocator.Find(
            "ffmpeg.exe",
            configuration.FfmpegPath,
            configuration.ToolDirectory);
        if (ytDlpPath is null || ffmpegPath is null)
        {
            throw new DownloadException("下载需要 yt-dlp.exe 和 ffmpeg.exe，请先完成工具检测。");
        }

        string outputDirectory = Path.GetFullPath(request.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        string desiredFileName = fileNameService.BuildFileName(request.Title);
        string desiredPath = fileNameService.ResolveCollision(outputDirectory, desiredFileName);
        string outputBaseName = Path.GetFileNameWithoutExtension(desiredPath);

        progress?.Report(new DownloadProgress(DownloadTaskState.Downloading, 0, Message: "开始下载媒体流"));
        IReadOnlyList<string> arguments;
        try
        {
            arguments = BuildArguments(
                request,
                outputDirectory,
                outputBaseName,
                Path.GetDirectoryName(ffmpegPath)!);
        }
        catch (VideoProbeException exception)
        {
            throw new DownloadException(exception.Message, exception);
        }

        InlineProgress<ProcessOutputLine> processProgress = new(line =>
        {
            if (progressParser.TryParse(line.Text, out DownloadProgress? parsed) && parsed is not null)
            {
                progress?.Report(parsed);
            }
        });

        ProcessResult processResult = await processRunner.RunAsync(
            new ProcessRequest
            {
                FileName = ytDlpPath,
                Arguments = arguments,
            },
            processProgress,
            cancellationToken).ConfigureAwait(false);
        if (!processResult.IsSuccess)
        {
            throw new DownloadException(BuildSafeError(processResult));
        }

        string? reportedPath = progressParser.FindOutputPath(processResult.StandardOutput);
        if (string.IsNullOrWhiteSpace(reportedPath))
        {
            throw new DownloadException("yt-dlp 未返回最终文件路径，无法确认下载结果。");
        }

        string outputPath = EnsurePathIsInsideOutputDirectory(reportedPath, outputDirectory);
        bool expectAudio = request.FormatSelection.Audio is not null || request.FormatSelection.Video.HasAudio;
        progress?.Report(new DownloadProgress(DownloadTaskState.Validating, Message: "正在校验 MP4 文件"));
        MediaValidationResult validation = await validationService.ValidateAsync(
            outputPath,
            request.ExpectedDuration,
            expectAudio,
            cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid)
        {
            throw new DownloadException(string.Join(" ", validation.Issues));
        }

        if (request.OutputMode == Mp4OutputMode.UniversalCompatibility && !validation.IsUniversalMp4)
        {
            progress?.Report(new DownloadProgress(DownloadTaskState.Transcoding, 0, Message: "正在生成通用兼容 MP4"));
            await transcodeService.TranscodeToUniversalMp4Async(
                outputPath,
                request.ExpectedDuration,
                progress,
                cancellationToken).ConfigureAwait(false);
            validation = await validationService.ValidateAsync(
                outputPath,
                request.ExpectedDuration,
                expectAudio,
                cancellationToken).ConfigureAwait(false);
            if (!validation.IsUniversalMp4)
            {
                throw new DownloadException("通用兼容 MP4 转码后校验失败。");
            }
        }

        progress?.Report(new DownloadProgress(DownloadTaskState.Completed, 100, Message: "下载完成"));
        return new DownloadResult(outputPath, validation);
    }

    private static IReadOnlyList<string> BuildArguments(
        DownloadRequest request,
        string outputDirectory,
        string outputBaseName,
        string ffmpegDirectory)
    {
        List<string> arguments =
        [
            "--format", request.FormatSelection.FormatExpression,
            "--merge-output-format", "mp4",
            "--remux-video", "mp4",
            "--ffmpeg-location", ffmpegDirectory,
            "--windows-filenames",
            "--newline",
            "--no-playlist",
            "--no-overwrites",
            "--progress-template", $"download:{YtDlpProgressParser.ProgressPrefix}%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s",
            "--print", $"after_move:{YtDlpProgressParser.FilePrefix}%(filepath)s",
            "-P", outputDirectory,
            "-o", $"{outputBaseName}.%(ext)s",
        ];

        YtDlpLoginArgumentBuilder.AddTo(arguments, request.LoginSource);
        arguments.Add("--");
        arguments.Add(request.Url);
        return arguments;
    }

    private static string EnsurePathIsInsideOutputDirectory(string reportedPath, string outputDirectory)
    {
        string fullPath = Path.GetFullPath(reportedPath);
        string relativePath = Path.GetRelativePath(outputDirectory, fullPath);
        if (Path.IsPathRooted(relativePath)
            || relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new DownloadException("下载工具返回了保存目录之外的文件路径，任务已终止。");
        }

        return fullPath;
    }

    private static string BuildSafeError(ProcessResult result)
    {
        string line = result.StandardError
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .FirstOrDefault(value => value.Length > 0)
            ?? $"下载进程退出码：{result.ExitCode}";
        return line.Length <= 500 ? line : line[..500];
    }
}
