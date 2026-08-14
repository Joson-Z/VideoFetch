using System.Globalization;
using VideoFetch.Application.Downloads;
using VideoFetch.Domain.Downloads;

namespace VideoFetch.Infrastructure.YtDlp;

public sealed class YtDlpProgressParser
{
    public const string ProgressPrefix = "__VIDEOFETCH_PROGRESS__:";
    public const string FilePrefix = "__VIDEOFETCH_FILE__:";

    public bool TryParse(string line, out DownloadProgress? progress)
    {
        progress = null;
        int markerIndex = line.IndexOf(ProgressPrefix, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        string payload = line[(markerIndex + ProgressPrefix.Length)..];
        string[] fields = payload.Split('|');
        double? percent = ParsePercent(fields.ElementAtOrDefault(0));
        string? speed = NormalizeField(fields.ElementAtOrDefault(1));
        string? eta = NormalizeField(fields.ElementAtOrDefault(2));
        progress = new DownloadProgress(DownloadTaskState.Downloading, percent, speed, eta);
        return true;
    }

    public string? FindOutputPath(string standardOutput)
    {
        foreach (string line in standardOutput
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                     .AsEnumerable()
                     .Reverse())
        {
            int markerIndex = line.IndexOf(FilePrefix, StringComparison.Ordinal);
            if (markerIndex >= 0)
            {
                return line[(markerIndex + FilePrefix.Length)..].Trim();
            }
        }

        return null;
    }

    private static double? ParsePercent(string? value)
    {
        string? normalized = NormalizeField(value)?.TrimEnd('%').Trim();
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? Math.Clamp(result, 0, 100)
            : null;
    }

    private static string? NormalizeField(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, "NA", StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;
    }
}
