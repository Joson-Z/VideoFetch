using VideoFetch.Application.Media;
using VideoFetch.Domain.Authentication;
using VideoFetch.Domain.Media;

namespace VideoFetch.Application.Downloads;

public sealed record DownloadRequest
{
    public required string Url { get; init; }

    public required string Title { get; init; }

    public required string OutputDirectory { get; init; }

    public required LoginSource LoginSource { get; init; }

    public required FormatSelection FormatSelection { get; init; }

    public Mp4OutputMode OutputMode { get; init; } = Mp4OutputMode.PreserveSourceQuality;

    public TimeSpan? ExpectedDuration { get; init; }
}
