using VideoFetch.Application.Processes;

namespace VideoFetch.Application.Tooling;

public sealed class ToolchainService(IExecutableLocator executableLocator, IProcessRunner processRunner)
    : IToolchainService
{
    private static readonly TimeSpan VersionCheckTimeout = TimeSpan.FromSeconds(8);

    public async Task<ToolchainReport> CheckAsync(
        ToolchainConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ToolCheckResult[] results =
        [
            await CheckComponentAsync(
                ToolComponent.YtDlp,
                "yt-dlp.exe",
                configuration.YtDlpPath,
                ["--version"],
                configuration.ToolDirectory,
                cancellationToken).ConfigureAwait(false),
            await CheckComponentAsync(
                ToolComponent.Ffmpeg,
                "ffmpeg.exe",
                configuration.FfmpegPath,
                ["-version"],
                configuration.ToolDirectory,
                cancellationToken).ConfigureAwait(false),
            await CheckComponentAsync(
                ToolComponent.Ffprobe,
                "ffprobe.exe",
                configuration.FfprobePath,
                ["-version"],
                configuration.ToolDirectory,
                cancellationToken).ConfigureAwait(false),
        ];

        return new ToolchainReport(results);
    }

    private async Task<ToolCheckResult> CheckComponentAsync(
        ToolComponent component,
        string executableName,
        string? explicitPath,
        IReadOnlyList<string> versionArguments,
        string? toolDirectory,
        CancellationToken cancellationToken)
    {
        string? path = executableLocator.Find(executableName, explicitPath, toolDirectory);
        if (path is null)
        {
            return new ToolCheckResult(
                component,
                executableName,
                null,
                false,
                null,
                $"未找到 {executableName}");
        }

        try
        {
            ProcessResult result = await processRunner.RunAsync(
                new ProcessRequest
                {
                    FileName = path,
                    Arguments = versionArguments,
                    Timeout = VersionCheckTimeout,
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            string version = FirstNonEmptyLine(result.StandardOutput, result.StandardError) ?? "版本未知";
            return result.IsSuccess
                ? new ToolCheckResult(component, executableName, path, true, version, null)
                : new ToolCheckResult(component, executableName, path, false, version, BuildError(result));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ToolCheckResult(component, executableName, path, false, null, "版本检测超时");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ToolCheckResult(component, executableName, path, false, null, exception.Message);
        }
    }

    private static string? FirstNonEmptyLine(params string[] values) =>
        values.SelectMany(value => value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0);

    private static string BuildError(ProcessResult result) =>
        FirstNonEmptyLine(result.StandardError, result.StandardOutput) ?? $"进程退出码：{result.ExitCode}";
}
