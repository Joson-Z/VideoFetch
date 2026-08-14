using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Infrastructure.Ffmpeg;

namespace VideoFetch.UnitTests.Ffmpeg;

[TestClass]
public sealed class FfprobeMediaValidationServiceTests
{
    [TestMethod]
    public async Task ValidateAsync_ForH264AacMp4_ReturnsUniversalValidResult()
    {
        string filePath = CreateTemporaryMediaFile();
        try
        {
            FfprobeMediaValidationService service = CreateService("""
                {
                  "streams": [
                    { "codec_type": "video", "codec_name": "h264" },
                    { "codec_type": "audio", "codec_name": "aac" }
                  ],
                  "format": { "format_name": "mov,mp4,m4a,3gp,3g2,mj2", "duration": "120.5" }
                }
                """);

            var result = await service.ValidateAsync(filePath, TimeSpan.FromSeconds(120));

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.IsUniversalMp4);
            Assert.AreEqual(TimeSpan.FromSeconds(120.5), result.Duration);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task ValidateAsync_WhenExpectedAudioIsMissing_ReturnsIssue()
    {
        string filePath = CreateTemporaryMediaFile();
        try
        {
            FfprobeMediaValidationService service = CreateService("""
                {
                  "streams": [{ "codec_type": "video", "codec_name": "hevc" }],
                  "format": { "format_name": "mp4", "duration": "60" }
                }
                """);

            var result = await service.ValidateAsync(filePath, expectAudio: true);

            Assert.IsFalse(result.IsValid);
            CollectionAssert.Contains(result.Issues.ToArray(), "输出文件缺少音频流。");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task ValidateAsync_WhenDurationDiffersBeyondTolerance_ReturnsIssue()
    {
        string filePath = CreateTemporaryMediaFile();
        try
        {
            FfprobeMediaValidationService service = CreateService("""
                {
                  "streams": [{ "codec_type": "video", "codec_name": "h264" }],
                  "format": { "format_name": "mp4", "duration": "30" }
                }
                """);

            var result = await service.ValidateAsync(filePath, TimeSpan.FromSeconds(60), expectAudio: false);

            Assert.IsFalse(result.IsValid);
            CollectionAssert.Contains(result.Issues.ToArray(), "输出时长与视频信息不一致。");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static FfprobeMediaValidationService CreateService(string json) =>
        new(
            new ToolchainConfiguration(),
            new FixedExecutableLocator(),
            new StaticProcessRunner(new ProcessResult(0, json, string.Empty)));

    private static string CreateTemporaryMediaFile()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"videofetch-{Guid.NewGuid():N}.mp4");
        File.WriteAllText(filePath, "not-empty");
        return filePath;
    }

    private sealed class FixedExecutableLocator : IExecutableLocator
    {
        public string? Find(string executableName, string? explicitPath = null, string? toolDirectory = null) =>
            Path.Combine("C:\\tools", executableName);
    }

    private sealed class StaticProcessRunner(ProcessResult result) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(
            ProcessRequest request,
            IProgress<ProcessOutputLine>? progress = null,
            CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
