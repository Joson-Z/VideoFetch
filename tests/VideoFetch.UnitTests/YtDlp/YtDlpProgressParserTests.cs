using VideoFetch.Infrastructure.YtDlp;

namespace VideoFetch.UnitTests.YtDlp;

[TestClass]
public sealed class YtDlpProgressParserTests
{
    private readonly YtDlpProgressParser parser = new();

    [TestMethod]
    public void TryParse_MapsPercentSpeedAndEta()
    {
        bool parsed = parser.TryParse(
            "download:__VIDEOFETCH_PROGRESS__: 42.5%|8.2MiB/s|00:36",
            out var progress);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(progress);
        Assert.AreEqual(42.5, progress.Percent);
        Assert.AreEqual("8.2MiB/s", progress.Speed);
        Assert.AreEqual("00:36", progress.EstimatedTimeRemaining);
    }

    [TestMethod]
    public void TryParse_ConvertsNaFieldsToNull()
    {
        bool parsed = parser.TryParse(
            "__VIDEOFETCH_PROGRESS__:NA|NA|NA",
            out var progress);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(progress);
        Assert.IsNull(progress.Percent);
        Assert.IsNull(progress.Speed);
        Assert.IsNull(progress.EstimatedTimeRemaining);
    }

    [TestMethod]
    public void FindOutputPath_ReturnsLastAfterMoveMarker()
    {
        string output = "__VIDEOFETCH_FILE__:C:\\Videos\\old.mp4\r\n__VIDEOFETCH_FILE__:C:\\Videos\\final.mp4\r\n";

        string? result = parser.FindOutputPath(output);

        Assert.AreEqual("C:\\Videos\\final.mp4", result);
    }
}
