namespace VideoFetch.Domain.Authentication;

/// <summary>
/// Describes where yt-dlp should obtain the user's existing authenticated session.
/// The application never accepts an account password.
/// </summary>
public abstract record LoginSource
{
    private LoginSource()
    {
    }

    public sealed record Browser(BrowserType Type, string? Profile = null) : LoginSource;

    public sealed record CookieFile(string Path) : LoginSource;

    public sealed record Anonymous : LoginSource;
}
