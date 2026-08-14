namespace VideoFetch.Application.Downloads;

public sealed record DownloadResult(string OutputPath, MediaValidationResult Validation);
