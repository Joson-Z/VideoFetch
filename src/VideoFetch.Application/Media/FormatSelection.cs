using VideoFetch.Domain.Media;

namespace VideoFetch.Application.Media;

public sealed record FormatSelection(MediaFormat Video, MediaFormat? Audio)
{
    public string FormatExpression => Audio is null
        ? Video.FormatId
        : $"{Video.FormatId}+{Audio.FormatId}";

    public int? Height => Video.Height;
}
