namespace VideoFetch.App.Services;

public interface IFileDialogService
{
    string? SelectCookieFile(string? currentPath);

    string? SelectFolder(string? currentPath, string title);
}
