namespace VideoFetch.Domain.Media;

/// <summary>
/// A normalized media format returned by a site extractor.
/// </summary>
public sealed record MediaFormat
{
    public required string FormatId { get; init; }

    public string? FormatNote { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public double? FramesPerSecond { get; init; }

    public string? VideoCodec { get; init; }

    public string? AudioCodec { get; init; }

    public double? VideoBitrateKbps { get; init; }

    public double? AudioBitrateKbps { get; init; }

    public double? TotalBitrateKbps { get; init; }

    public string? Extension { get; init; }

    public long? FileSizeBytes { get; init; }

    public string? DynamicRange { get; init; }

    public string? Language { get; init; }

    public bool HasVideo => !IsMissingCodec(VideoCodec);

    public bool HasAudio => !IsMissingCodec(AudioCodec);

    public bool IsVideoOnly => HasVideo && !HasAudio;

    public bool IsAudioOnly => HasAudio && !HasVideo;

    private static bool IsMissingCodec(string? codec) =>
        string.IsNullOrWhiteSpace(codec) || string.Equals(codec, "none", StringComparison.OrdinalIgnoreCase);
}
