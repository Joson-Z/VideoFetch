using System.Text.Json;
using VideoFetch.Application.Media;
using VideoFetch.Domain.Media;

namespace VideoFetch.Infrastructure.YtDlp;

public sealed class YtDlpMetadataParser
{
    public VideoMetadata Parse(string json, string requestedUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedUrl);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            return new VideoMetadata
            {
                Id = GetRequiredString(root, "id"),
                Title = GetRequiredString(root, "title"),
                OriginalUrl = GetString(root, "webpage_url")
                    ?? GetString(root, "original_url")
                    ?? requestedUrl,
                Uploader = GetString(root, "uploader") ?? GetString(root, "channel"),
                ThumbnailUrl = GetString(root, "thumbnail"),
                Duration = GetDouble(root, "duration") is { } duration
                    ? TimeSpan.FromSeconds(duration)
                    : null,
                Parts = ParseParts(root),
                Formats = ParseFormats(root),
            };
        }
        catch (JsonException exception)
        {
            throw new VideoProbeException("yt-dlp 返回了无法解析的视频信息。", exception);
        }
    }

    private static IReadOnlyList<VideoPart> ParseParts(JsonElement root)
    {
        if (!root.TryGetProperty("entries", out JsonElement entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<VideoPart> parts = [];
        int index = 1;
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string title = GetString(entry, "title") ?? $"P{index}";
            string url = GetString(entry, "webpage_url") ?? GetString(entry, "url") ?? string.Empty;
            parts.Add(new VideoPart(index, title, url));
            index++;
        }

        return parts;
    }

    private static IReadOnlyList<MediaFormat> ParseFormats(JsonElement root)
    {
        if (!root.TryGetProperty("formats", out JsonElement formats) || formats.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<MediaFormat> result = [];
        foreach (JsonElement format in formats.EnumerateArray())
        {
            string? formatId = GetString(format, "format_id");
            if (string.IsNullOrWhiteSpace(formatId))
            {
                continue;
            }

            result.Add(new MediaFormat
            {
                FormatId = formatId,
                FormatNote = GetString(format, "format_note"),
                Width = GetInt32(format, "width"),
                Height = GetInt32(format, "height"),
                FramesPerSecond = GetDouble(format, "fps"),
                VideoCodec = GetString(format, "vcodec"),
                AudioCodec = GetString(format, "acodec"),
                VideoBitrateKbps = GetDouble(format, "vbr"),
                AudioBitrateKbps = GetDouble(format, "abr"),
                TotalBitrateKbps = GetDouble(format, "tbr"),
                Extension = GetString(format, "ext"),
                FileSizeBytes = GetInt64(format, "filesize") ?? GetInt64(format, "filesize_approx"),
                DynamicRange = GetString(format, "dynamic_range"),
                Language = GetString(format, "language"),
            });
        }

        return result;
    }

    private static string GetRequiredString(JsonElement element, string propertyName) =>
        GetString(element, propertyName)
        ?? throw new VideoProbeException($"视频信息缺少必要字段：{propertyName}");

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static int? GetInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.TryGetInt32(out int result)
            ? result
            : null;

    private static long? GetInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.TryGetInt64(out long result)
            ? result
            : null;

    private static double? GetDouble(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.TryGetDouble(out double result)
            ? result
            : null;
}
