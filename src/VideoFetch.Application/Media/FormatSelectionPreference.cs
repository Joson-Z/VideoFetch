using VideoFetch.Domain.Media;

namespace VideoFetch.Application.Media;

public sealed record FormatSelectionPreference
{
    public int? MaximumHeight { get; init; }

    public Mp4OutputMode OutputMode { get; init; } = Mp4OutputMode.PreserveSourceQuality;
}
