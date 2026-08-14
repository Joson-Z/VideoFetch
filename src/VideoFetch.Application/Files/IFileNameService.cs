namespace VideoFetch.Application.Files;

public interface IFileNameService
{
    string SanitizeTitle(string? title, int maxLength = 180);

    string BuildFileName(string? title, string extension = ".mp4", int maxTitleLength = 180);

    string ResolveCollision(string directory, string fileName);
}
