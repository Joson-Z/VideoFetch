using VideoFetch.Infrastructure.Files;

namespace VideoFetch.UnitTests.Files;

[TestClass]
public sealed class WindowsFileNameServiceTests
{
    private readonly WindowsFileNameService service = new();

    [TestMethod]
    public void SanitizeTitle_ReplacesWindowsInvalidCharacters()
    {
        string result = service.SanitizeTitle("A<B>:C\"/D\\E|F?G*");

        Assert.AreEqual("A_B__C__D_E_F_G_", result);
    }

    [TestMethod]
    [DataRow("CON", "_CON")]
    [DataRow("con.txt", "_con.txt")]
    [DataRow("LPT9", "_LPT9")]
    [DataRow("COM1.mp4", "_COM1.mp4")]
    public void SanitizeTitle_PrefixesReservedDeviceNames(string title, string expected)
    {
        Assert.AreEqual(expected, service.SanitizeTitle(title));
    }

    [TestMethod]
    public void SanitizeTitle_UsesFallbackForEmptyTitle()
    {
        Assert.AreEqual("未命名视频", service.SanitizeTitle("  ...  "));
    }

    [TestMethod]
    public void SanitizeTitle_DoesNotSplitSurrogatePair()
    {
        string result = service.SanitizeTitle("A😀B", 2);

        Assert.AreEqual("A", result);
    }

    [TestMethod]
    public void BuildFileName_NormalizesExtension()
    {
        Assert.AreEqual("标题.mp4", service.BuildFileName("标题", "mp4"));
    }

    [TestMethod]
    public void ResolveCollision_AppendsFirstAvailableSuffix()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "标题.mp4"), string.Empty);
            File.WriteAllText(Path.Combine(directory, "标题 (1).mp4"), string.Empty);

            string result = service.ResolveCollision(directory, "标题.mp4");

            Assert.AreEqual(Path.Combine(directory, "标题 (2).mp4"), result);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "VideoFetch.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
