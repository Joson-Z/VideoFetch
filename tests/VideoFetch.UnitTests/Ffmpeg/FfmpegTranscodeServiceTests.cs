using VideoFetch.Application.Downloads;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Infrastructure.Ffmpeg;

namespace VideoFetch.UnitTests.Ffmpeg;

[TestClass]
public sealed class FfmpegTranscodeServiceTests
{
    [TestMethod]
    public async Task TranscodeToUniversalMp4Async_ReplacesOriginalAfterSuccessfulProcess()
    {
        string directory = Path.Combine(Path.GetTempPath(), "VideoFetch.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string inputPath = Path.Combine(directory, "video.mp4");
        await File.WriteAllTextAsync(inputPath, "original");
        try
        {
            CreatingProcessRunner runner = new();
            FfmpegTranscodeService service = new(
                new ToolchainConfiguration(),
                new FixedExecutableLocator(),
                runner);

            await service.TranscodeToUniversalMp4Async(inputPath, TimeSpan.FromMinutes(1));

            Assert.AreEqual("transcoded", await File.ReadAllTextAsync(inputPath));
            ProcessRequest request = runner.Requests.Single();
            CollectionAssert.Contains(request.Arguments.ToArray(), "libx264");
            CollectionAssert.Contains(request.Arguments.ToArray(), "aac");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FixedExecutableLocator : IExecutableLocator
    {
        public string? Find(string executableName, string? explicitPath = null, string? toolDirectory = null) =>
            Path.Combine("C:\\tools", executableName);
    }

    private sealed class CreatingProcessRunner : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = [];

        public async Task<ProcessResult> RunAsync(
            ProcessRequest request,
            IProgress<ProcessOutputLine>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            string outputPath = request.Arguments[^1];
            await File.WriteAllTextAsync(outputPath, "transcoded", cancellationToken);
            progress?.Report(new ProcessOutputLine("out_time=00:00:30.000000", false));
            return new ProcessResult(0, string.Empty, string.Empty);
        }
    }
}
