namespace VideoFetch.Application.Downloads;

public sealed record MediaValidationResult
{
    public required string FilePath { get; init; }

    public bool IsReadable { get; init; }

    public bool IsMp4 { get; init; }

    public bool HasVideo { get; init; }

    public bool HasAudio { get; init; }

    public string? VideoCodec { get; init; }

    public string? AudioCodec { get; init; }

    public TimeSpan? Duration { get; init; }

    public IReadOnlyList<string> Issues { get; init; } = [];

    public bool IsValid => IsReadable && IsMp4 && HasVideo && Issues.Count == 0;

    public bool IsUniversalMp4 =>
        IsValid
        && string.Equals(VideoCodec, "h264", StringComparison.OrdinalIgnoreCase)
        && (!HasAudio || string.Equals(AudioCodec, "aac", StringComparison.OrdinalIgnoreCase));
}
