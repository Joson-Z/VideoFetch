using VideoFetch.Application.Media;
using VideoFetch.Domain.Media;
using VideoFetch.Infrastructure.YtDlp;

namespace VideoFetch.UnitTests.YtDlp;

[TestClass]
public sealed class YtDlpMetadataParserTests
{
    private readonly YtDlpMetadataParser parser = new();

    [TestMethod]
    public void Parse_MapsMetadataFormatsAndParts()
    {
        const string json = """
            {
              "id": "BV1TEST",
              "title": "测试视频",
              "webpage_url": "https://www.bilibili.com/video/BV1TEST",
              "uploader": "测试作者",
              "thumbnail": "https://i.example/cover.jpg",
              "duration": 125.5,
              "entries": [
                { "title": "第一集", "webpage_url": "https://www.bilibili.com/video/BV1TEST?p=1" }
              ],
              "formats": [
                {
                  "format_id": "video-1080",
                  "format_note": "1080P",
                  "width": 1920,
                  "height": 1080,
                  "fps": 60,
                  "vcodec": "avc1.64002a",
                  "acodec": "none",
                  "vbr": 5000.5,
                  "tbr": 5000.5,
                  "ext": "mp4",
                  "filesize_approx": 123456789,
                  "dynamic_range": "SDR"
                },
                {
                  "format_id": "audio-192",
                  "vcodec": "none",
                  "acodec": "mp4a.40.2",
                  "abr": 192,
                  "ext": "m4a"
                }
              ]
            }
            """;

        VideoMetadata result = parser.Parse(json, "https://fallback.invalid");

        Assert.AreEqual("BV1TEST", result.Id);
        Assert.AreEqual("测试视频", result.Title);
        Assert.AreEqual("测试作者", result.Uploader);
        Assert.AreEqual(TimeSpan.FromSeconds(125.5), result.Duration);
        Assert.HasCount(1, result.Parts);
        Assert.HasCount(2, result.Formats);
        Assert.AreEqual(1080, result.Formats[0].Height);
        Assert.AreEqual(123456789L, result.Formats[0].FileSizeBytes);
        Assert.IsTrue(result.Formats[1].IsAudioOnly);
    }

    [TestMethod]
    public void Parse_WhenJsonIsInvalid_WrapsJsonException()
    {
        VideoProbeException exception = Assert.ThrowsExactly<VideoProbeException>(() =>
            parser.Parse("{invalid", "https://www.bilibili.com/video/BV1"));

        Assert.IsNotNull(exception.InnerException);
    }

    [TestMethod]
    public void Parse_WhenRequiredFieldIsMissing_ThrowsProbeException()
    {
        VideoProbeException exception = Assert.ThrowsExactly<VideoProbeException>(() =>
            parser.Parse("{\"title\":\"标题\"}", "https://www.bilibili.com/video/BV1"));

        StringAssert.Contains(exception.Message, "id");
    }
}
