using VideoFetch.Application.Media;
using VideoFetch.Domain.Media;

namespace VideoFetch.UnitTests.Media;

[TestClass]
public sealed class FormatSelectionServiceTests
{
    private readonly FormatSelectionService service = new();

    [TestMethod]
    public void Select_ChoosesHighestResolutionAndBestAudio()
    {
        MediaFormat video1080 = Video("1080", 1080, 60, 5_000, "avc1", "mp4");
        MediaFormat video2160 = Video("2160", 2160, 30, 10_000, "hev1", "mp4");
        MediaFormat audio128 = Audio("a128", 128);
        MediaFormat audio192 = Audio("a192", 192);

        FormatSelection result = service.Select(
            [video1080, audio128, video2160, audio192],
            new FormatSelectionPreference());

        Assert.AreEqual("2160", result.Video.FormatId);
        Assert.AreEqual("a192", result.Audio?.FormatId);
        Assert.AreEqual("2160+a192", result.FormatExpression);
    }

    [TestMethod]
    public void Select_RespectsMaximumHeight()
    {
        FormatSelection result = service.Select(
            [Video("720", 720, 30, 2_000), Video("1080", 1080, 30, 4_000), Video("2160", 2160, 30, 8_000)],
            new FormatSelectionPreference { MaximumHeight = 1080 });

        Assert.AreEqual("1080", result.Video.FormatId);
    }

    [TestMethod]
    public void Select_PrefersFrameRateBeforeBitrateAtSameResolution()
    {
        FormatSelection result = service.Select(
            [Video("high-bitrate", 1080, 30, 9_000), Video("high-fps", 1080, 60, 5_000)],
            new FormatSelectionPreference());

        Assert.AreEqual("high-fps", result.Video.FormatId);
    }

    [TestMethod]
    public void Select_InUniversalModePrefersAvcMp4AtSameQualityTier()
    {
        FormatSelection result = service.Select(
            [
                Video("av1", 1080, 60, 8_000, "av01.0.08M.08", "mp4"),
                Video("avc", 1080, 60, 5_000, "avc1.64002a", "mp4"),
            ],
            new FormatSelectionPreference { OutputMode = Mp4OutputMode.UniversalCompatibility });

        Assert.AreEqual("avc", result.Video.FormatId);
    }

    [TestMethod]
    public void Select_WhenNoAudioOnlyFormatExists_UsesCombinedFallback()
    {
        MediaFormat videoOnly = Video("video-only", 1080, 60, 5_000);
        MediaFormat combined = Video("combined", 720, 30, 2_000) with { AudioCodec = "aac" };

        FormatSelection result = service.Select(
            [videoOnly, combined],
            new FormatSelectionPreference());

        Assert.AreEqual("combined", result.Video.FormatId);
        Assert.IsNull(result.Audio);
    }

    [TestMethod]
    public void Select_WhenNoVideoMatches_ThrowsMeaningfulError()
    {
        FormatSelectionException exception = Assert.ThrowsExactly<FormatSelectionException>(() =>
            service.Select([Audio("audio", 192)], new FormatSelectionPreference()));

        StringAssert.Contains(exception.Message, "视频格式");
    }

    private static MediaFormat Video(
        string id,
        int height,
        double fps,
        double bitrate,
        string codec = "avc1",
        string extension = "mp4") =>
        new()
        {
            FormatId = id,
            Height = height,
            FramesPerSecond = fps,
            VideoBitrateKbps = bitrate,
            VideoCodec = codec,
            AudioCodec = "none",
            Extension = extension,
        };

    private static MediaFormat Audio(string id, double bitrate) =>
        new()
        {
            FormatId = id,
            VideoCodec = "none",
            AudioCodec = "mp4a.40.2",
            AudioBitrateKbps = bitrate,
            Extension = "m4a",
        };
}
