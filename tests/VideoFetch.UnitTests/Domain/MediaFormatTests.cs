using VideoFetch.Domain.Media;

namespace VideoFetch.UnitTests.Domain;

[TestClass]
public sealed class MediaFormatTests
{
    [TestMethod]
    public void VideoOnlyFormat_IsClassifiedCorrectly()
    {
        MediaFormat format = new()
        {
            FormatId = "video",
            VideoCodec = "avc1.640028",
            AudioCodec = "none",
        };

        Assert.IsTrue(format.HasVideo);
        Assert.IsFalse(format.HasAudio);
        Assert.IsTrue(format.IsVideoOnly);
        Assert.IsFalse(format.IsAudioOnly);
    }

    [TestMethod]
    public void AudioOnlyFormat_IsClassifiedCorrectly()
    {
        MediaFormat format = new()
        {
            FormatId = "audio",
            VideoCodec = "none",
            AudioCodec = "mp4a.40.2",
        };

        Assert.IsFalse(format.HasVideo);
        Assert.IsTrue(format.HasAudio);
        Assert.IsFalse(format.IsVideoOnly);
        Assert.IsTrue(format.IsAudioOnly);
    }

    [TestMethod]
    public void CombinedFormat_HasBothStreams()
    {
        MediaFormat format = new()
        {
            FormatId = "combined",
            VideoCodec = "avc1",
            AudioCodec = "aac",
        };

        Assert.IsTrue(format.HasVideo);
        Assert.IsTrue(format.HasAudio);
        Assert.IsFalse(format.IsVideoOnly);
        Assert.IsFalse(format.IsAudioOnly);
    }
}
