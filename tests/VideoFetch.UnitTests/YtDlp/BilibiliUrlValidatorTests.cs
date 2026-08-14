using VideoFetch.Infrastructure.YtDlp;

namespace VideoFetch.UnitTests.YtDlp;

[TestClass]
public sealed class BilibiliUrlValidatorTests
{
    private readonly BilibiliUrlValidator validator = new();

    [TestMethod]
    [DataRow("https://www.bilibili.com/video/BV123")]
    [DataRow("https://m.bilibili.com/video/BV123")]
    [DataRow("https://b23.tv/abc123")]
    public void IsSupported_AcceptsOfficialHosts(string url)
    {
        Assert.IsTrue(validator.IsSupported(url));
    }

    [TestMethod]
    [DataRow("file:///C:/secret.txt")]
    [DataRow("https://bilibili.com.example.org/video/BV123")]
    [DataRow("https://evilbilibili.com/video/BV123")]
    [DataRow("not-a-url")]
    public void IsSupported_RejectsUntrustedUrls(string url)
    {
        Assert.IsFalse(validator.IsSupported(url));
    }
}
