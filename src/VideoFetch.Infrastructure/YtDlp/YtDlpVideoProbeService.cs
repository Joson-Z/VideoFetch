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
            throw new VideoProbeException(BuildSafeError(result));
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

        switch (loginSource)
        {
            case LoginSource.Browser browser:
                arguments.Add("--cookies-from-browser");
                arguments.Add(BuildBrowserSpecifier(browser));
                break;
            case LoginSource.CookieFile cookieFile:
                if (!File.Exists(cookieFile.Path))
                {
                    throw new VideoProbeException("所选 Cookie 文件不存在。");
                }

                arguments.Add("--cookies");
                arguments.Add(Path.GetFullPath(cookieFile.Path));
                break;
            case LoginSource.Anonymous:
                break;
        }

        arguments.Add("--");
        arguments.Add(url);
        return arguments;
    }

    private static string BuildBrowserSpecifier(LoginSource.Browser browser)
    {
        string browserName = browser.Type switch
        {
            BrowserType.Edge => "edge",
            BrowserType.Chrome => "chrome",
            _ => throw new VideoProbeException("不支持所选浏览器。"),
        };

        return string.IsNullOrWhiteSpace(browser.Profile)
            ? browserName
            : $"{browserName}:{browser.Profile.Trim()}";
    }

    private static string BuildSafeError(ProcessResult result)
    {
        string firstLine = result.StandardError
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0)
            ?? "无法读取视频信息。";

        return firstLine.Length <= 500 ? firstLine : firstLine[..500];
    }
}
