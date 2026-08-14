namespace VideoFetch.Domain.Downloads;

public enum DownloadTaskState
{
    Created,
    Probing,
    Ready,
    Queued,
    Downloading,
    Merging,
    Remuxing,
    Transcoding,
    Validating,
    Completed,
    Cancelling,
    Cancelled,
    Failed,
}
