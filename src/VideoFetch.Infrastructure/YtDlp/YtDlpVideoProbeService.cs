using VideoFetch.Application.Media;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Domain.Authentication;
using VideoFetch.Domain.Media;

namespace VideoFetch.Infrastructure.YtDlp;

public sealed class YtDlpVideoProbeService(
    YtDlpConfiguration configuration,
    IExecutableLocator executableLocator,
    IProcessRunner processRunner,
    IVideoUrlValidator urlValidator,
    YtDlpMetadataParser metadataParser) : IVideoProbeService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(45);

    public async Task<VideoMetadata> ProbeAsync(
        string url,
        LoginSource loginSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loginSource);
        if (!urlValidator.IsSupported(url))
        {
            throw new VideoProbeException("请输入有效的 B 站视频链接。");
        }

        string? executable = executableLocator.Find(
            "yt-dlp.exe",
            configuration.ExecutablePath,
            configuration.ToolDirectory);
        if (executable is null)
        {
            throw new VideoProbeException("未找到 yt-dlp.exe，请先在设置中配置工具路径。");
        }

        IReadOnlyList<string> arguments = BuildArguments(url, loginSource);
        ProcessResult result = await processRunner.RunAsync(
            new ProcessRequest
            {
                FileName = executable,
                Arguments = arguments,
                Timeout = ProbeTimeout,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw new VideoProbeException(BuildSafeError(result, loginSource));
        }

        return metadataParser.Parse(result.StandardOutput, url);
    }

    internal static IReadOnlyList<string> BuildArguments(string url, LoginSource loginSource)
    {
        List<string> arguments =
        [
            "--dump-single-json",
            "--skip-download",
            "--no-warnings",
            "--no-playlist",
        ];

        YtDlpLoginArgumentBuilder.AddTo(arguments, loginSource);

        arguments.Add("--");
        arguments.Add(url);
        return arguments;
    }

    private static string BuildSafeError(ProcessResult result, LoginSource loginSource)
    {
        string message = YtDlpErrorMessageBuilder.Build(result, "无法读取视频信息。");
        return UseSelectedBrowserName(message, loginSource);
    }

    private static string UseSelectedBrowserName(string message, LoginSource loginSource)
    {
        if (loginSource is not LoginSource.Browser browser
            || !message.StartsWith("无法读取 ", StringComparison.Ordinal))
        {
            return message;
        }

        string selectedBrowser = browser.Type == BrowserType.Edge ? "Edge" : "Chrome";
        return message
            .Replace("Chrome", selectedBrowser, StringComparison.Ordinal)
            .Replace("Edge", selectedBrowser, StringComparison.Ordinal);
    }
}
