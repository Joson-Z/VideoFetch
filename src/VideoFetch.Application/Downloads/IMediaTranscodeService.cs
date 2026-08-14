namespace VideoFetch.Application.Downloads;

public interface IMediaTranscodeService
{
    Task TranscodeToUniversalMp4Async(
        string inputPath,
        TimeSpan? duration,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
