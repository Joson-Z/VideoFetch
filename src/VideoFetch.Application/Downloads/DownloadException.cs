namespace VideoFetch.Application.Downloads;

public sealed class DownloadException : Exception
{
    public DownloadException(string message)
        : base(message)
    {
    }

    public DownloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
