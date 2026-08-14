using VideoFetch.Domain.Downloads;

namespace VideoFetch.Application.Downloads;

public sealed record DownloadProgress(
    DownloadTaskState State,
    double? Percent = null,
    string? Speed = null,
    string? EstimatedTimeRemaining = null,
    string? Message = null);
