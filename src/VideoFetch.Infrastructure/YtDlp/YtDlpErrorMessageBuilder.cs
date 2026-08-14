using VideoFetch.Application.Processes;

namespace VideoFetch.Infrastructure.YtDlp;

internal static class YtDlpErrorMessageBuilder
{
    public static string Build(ProcessResult result, string fallback)
    {
        string error = result.StandardError.Trim();
        if (IsCookieDatabaseLocked(error))
        {
            string browser = error.Contains("Edge", StringComparison.OrdinalIgnoreCase)
                ? "Edge"
                : "Chrome";
            return $"无法读取 {browser} 登录信息：浏览器正在占用 Cookie 数据库。请完全退出 {browser}（包括后台进程）后重试，或改用 cookies.txt。";
        }

        if (error.Contains("Failed to decrypt with DPAPI", StringComparison.OrdinalIgnoreCase)
            || error.Contains("cookie decryption failed", StringComparison.OrdinalIgnoreCase))
        {
            return "无法解密浏览器登录信息。请在当前 Windows 用户下运行程序，或改用 cookies.txt。";
        }

        string firstLine = error
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0)
            ?? fallback;
        return firstLine.Length <= 500 ? firstLine : firstLine[..500];
    }

    private static bool IsCookieDatabaseLocked(string error) =>
        error.Contains("Could not copy Chrome cookie database", StringComparison.OrdinalIgnoreCase)
        || error.Contains("Could not copy Edge cookie database", StringComparison.OrdinalIgnoreCase)
        || (error.Contains("Permission denied", StringComparison.OrdinalIgnoreCase)
            && error.Contains("Cookies", StringComparison.OrdinalIgnoreCase));
}
