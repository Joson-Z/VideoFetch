using Microsoft.Win32;
using System.IO;

namespace VideoFetch.App.Services;

public sealed class WindowsFileDialogService : IFileDialogService
{
    public string? SelectCookieFile(string? currentPath)
    {
        OpenFileDialog dialog = new()
        {
            Title = "选择 Netscape 格式 Cookie 文件",
            Filter = "Cookie 文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            FileName = currentPath ?? string.Empty,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectFolder(string? currentPath, string title)
    {
        OpenFolderDialog dialog = new()
        {
            Title = title,
            InitialDirectory = Directory.Exists(currentPath) ? currentPath : null,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
