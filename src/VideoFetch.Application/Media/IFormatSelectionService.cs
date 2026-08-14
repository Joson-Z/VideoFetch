using VideoFetch.Domain.Media;

namespace VideoFetch.Application.Media;

public interface IFormatSelectionService
{
    FormatSelection Select(
        IReadOnlyCollection<MediaFormat> formats,
        FormatSelectionPreference preference);
}
