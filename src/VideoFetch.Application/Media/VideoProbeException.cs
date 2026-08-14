namespace VideoFetch.Application.Media;

public sealed class VideoProbeException : Exception
{
    public VideoProbeException(string message)
        : base(message)
    {
    }

    public VideoProbeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
