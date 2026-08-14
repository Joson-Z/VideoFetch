using VideoFetch.Application.Media;
using VideoFetch.Domain.Authentication;

namespace VideoFetch.Infrastructure.YtDlp;

internal static class YtDlpLoginArgumentBuilder
{
    public static void AddTo(ICollection<string> arguments, LoginSource loginSource)
    {
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
}
