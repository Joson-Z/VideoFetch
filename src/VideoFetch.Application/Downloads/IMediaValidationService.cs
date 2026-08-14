namespace VideoFetch.Application.Downloads;

public interface IMediaValidationService
{
    Task<MediaValidationResult> ValidateAsync(
        string filePath,
        TimeSpan? expectedDuration = null,
        bool expectAudio = true,
        CancellationToken cancellationToken = default);
}
