using VideoFetch.Application.Media;

namespace VideoFetch.Infrastructure.YtDlp;

public sealed class BilibiliUrlValidator : IVideoUrlValidator
{
    private static readonly string[] SupportedHosts = ["bilibili.com", "b23.tv"];

    public bool IsSupported(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        return SupportedHosts.Any(host =>
            string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase));
    }
}
