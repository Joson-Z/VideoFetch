namespace VideoFetch.Application.Media;

public interface IVideoUrlValidator
{
    bool IsSupported(string url);
}
