using VideoFetch.Infrastructure.Tooling;

namespace VideoFetch.UnitTests.Tooling;

[TestClass]
public sealed class WindowsExecutableLocatorTests
{
    [TestMethod]
    public void Find_ReturnsExplicitExecutablePath()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string executable = Path.Combine(directory, "yt-dlp.exe");
            File.WriteAllBytes(executable, []);
            WindowsExecutableLocator locator = new();

            string? result = locator.Find("yt-dlp.exe", executable);

            Assert.AreEqual(Path.GetFullPath(executable), result);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Find_UsesConfiguredToolDirectory()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string executable = Path.Combine(directory, "ffmpeg.exe");
            File.WriteAllBytes(executable, []);
            WindowsExecutableLocator locator = new();

            string? result = locator.Find("ffmpeg.exe", toolDirectory: directory);

            Assert.AreEqual(Path.GetFullPath(executable), result);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Find_ReturnsNullWhenExecutableDoesNotExist()
    {
        WindowsExecutableLocator locator = new();

        string? result = locator.Find($"missing-{Guid.NewGuid():N}.exe");

        Assert.IsNull(result);
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "VideoFetch.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
