using System.Text;
using VideoFetch.Application.Files;

namespace VideoFetch.Infrastructure.Files;

public sealed class WindowsFileNameService : IFileNameService
{
    private const string DefaultTitle = "未命名视频";
    private static readonly HashSet<char> InvalidCharacters = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
    private static readonly HashSet<string> ReservedNames = BuildReservedNames();

    public string SanitizeTitle(string? title, int maxLength = 180)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);

        string source = string.IsNullOrWhiteSpace(title) ? DefaultTitle : title.Trim();
        StringBuilder builder = new(source.Length);

        foreach (char character in source)
        {
            builder.Append(character < 32 || InvalidCharacters.Contains(character) ? '_' : character);
        }

        string sanitized = builder.ToString().TrimEnd(' ', '.');
        if (sanitized.Length == 0)
        {
            sanitized = DefaultTitle;
        }

        sanitized = TruncateWithoutSplittingSurrogatePair(sanitized, maxLength).TrimEnd(' ', '.');
        string deviceName = sanitized.Split('.', 2)[0];
        if (ReservedNames.Contains(deviceName))
        {
            sanitized = $"_{sanitized}";
        }

        return sanitized;
    }

    public string BuildFileName(string? title, string extension = ".mp4", int maxTitleLength = 180)
    {
        string normalizedExtension = NormalizeExtension(extension);
        return $"{SanitizeTitle(title, maxTitleLength)}{normalizedExtension}";
    }

    public string ResolveCollision(string directory, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        for (int suffix = 1; suffix < int.MaxValue; suffix++)
        {
            candidate = Path.Combine(directory, $"{baseName} ({suffix}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("无法为输出文件生成唯一名称。");
    }

    private static string NormalizeExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        string normalized = extension.Trim();
        return normalized.StartsWith('.') ? normalized : $".{normalized}";
    }

    private static string TruncateWithoutSplittingSurrogatePair(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        int length = maxLength;
        if (length > 0 && char.IsHighSurrogate(value[length - 1]))
        {
            length--;
        }

        return value[..length];
    }

    private static HashSet<string> BuildReservedNames()
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
        };

        for (int index = 1; index <= 9; index++)
        {
            names.Add($"COM{index}");
            names.Add($"LPT{index}");
        }

        return names;
    }
}
