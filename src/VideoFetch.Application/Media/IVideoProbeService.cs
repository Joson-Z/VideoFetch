using VideoFetch.Domain.Authentication;
using VideoFetch.Domain.Media;

namespace VideoFetch.Application.Media;

public interface IVideoProbeService
{
    Task<VideoMetadata> ProbeAsync(
        string url,
        LoginSource loginSource,
        CancellationToken cancellationToken = default);
}
