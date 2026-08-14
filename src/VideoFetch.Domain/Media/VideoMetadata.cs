namespace VideoFetch.Domain.Media;

/// <summary>
/// Metadata and formats available to the current user session.
/// </summary>
public sealed record VideoMetadata
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string OriginalUrl { get; init; }

    public string? Uploader { get; init; }

    public string? ThumbnailUrl { get; init; }

    public TimeSpan? Duration { get; init; }

    public IReadOnlyList<VideoPart> Parts { get; init; } = [];

    public IReadOnlyList<MediaFormat> Formats { get; init; } = [];
}
