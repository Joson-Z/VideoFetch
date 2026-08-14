using VideoFetch.Application.Downloads;
using VideoFetch.Application.Media;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Domain.Authentication;
using VideoFetch.Domain.Media;
using VideoFetch.Infrastructure.Files;
using VideoFetch.Infrastructure.YtDlp;

namespace VideoFetch.UnitTests.YtDlp;

[TestClass]
public sealed class YtDlpDownloadServiceTests
{
    [TestMethod]
    public async Task DownloadAsync_BuildsExplicitFormatAndMp4Arguments()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string outputPath = Path.Combine(directory, "测试标题.mp4");
            CapturingProcessRunner runner = new(outputPath);
            QueueValidationService validator = new(ValidResult(outputPath, universal: true));
            CountingTranscodeService transcoder = new();
            YtDlpDownloadService service = CreateService(runner, validator, transcoder);

            DownloadResult result = await service.DownloadAsync(CreateRequest(directory));

            Assert.AreEqual(outputPath, result.OutputPath);
            ProcessRequest processRequest = runner.Requests.Single();
            AssertArgumentPair(processRequest.Arguments, "--format", "video+audio");
            AssertArgumentPair(processRequest.Arguments, "--merge-output-format", "mp4");
            AssertArgumentPair(processRequest.Arguments, "--remux-video", "mp4");
            Assert.AreEqual("--", processRequest.Arguments[^2]);
            Assert.AreEqual("https://www.bilibili.com/video/BV1TEST", processRequest.Arguments[^1]);
            Assert.AreEqual(0, transcoder.CallCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task DownloadAsync_InUniversalModeTranscodesAndRevalidates()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string outputPath = Path.Combine(directory, "测试标题.mp4");
            CapturingProcessRunner runner = new(outputPath);
            QueueValidationService validator = new(
                ValidResult(outputPath, universal: false),
                ValidResult(outputPath, universal: true));
            CountingTranscodeService transcoder = new();
            YtDlpDownloadService service = CreateService(runner, validator, transcoder);
            DownloadRequest request = CreateRequest(directory) with
            {
                OutputMode = Mp4OutputMode.UniversalCompatibility,
            };

            DownloadResult result = await service.DownloadAsync(request);

            Assert.IsTrue(result.Validation.IsUniversalMp4);
            Assert.AreEqual(1, transcoder.CallCount);
            Assert.AreEqual(2, validator.CallCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task DownloadAsync_WhenReportedPathEscapesOutputDirectory_RejectsResult()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string outsidePath = Path.Combine(Path.GetTempPath(), $"outside-{Guid.NewGuid():N}.mp4");
            CapturingProcessRunner runner = new(outsidePath);
            QueueValidationService validator = new(ValidResult(outsidePath, universal: true));
            YtDlpDownloadService service = CreateService(runner, validator, new CountingTranscodeService());

            DownloadException exception = await Assert.ThrowsExactlyAsync<DownloadException>(() =>
                service.DownloadAsync(CreateRequest(directory)));

            StringAssert.Contains(exception.Message, "保存目录之外");
            Assert.AreEqual(0, validator.CallCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static YtDlpDownloadService CreateService(
        IProcessRunner processRunner,
        IMediaValidationService validator,
        IMediaTranscodeService transcoder) =>
        new(
            new YtDlpConfiguration { ToolDirectory = "C:\\tools" },
            new FixedExecutableLocator(),
            processRunner,
            new WindowsFileNameService(),
            validator,
            transcoder,
            new YtDlpProgressParser());

    private static DownloadRequest CreateRequest(string directory) =>
        new()
        {
            Url = "https://www.bilibili.com/video/BV1TEST",
            Title = "测试标题",
            OutputDirectory = directory,
            LoginSource = new LoginSource.Browser(BrowserType.Edge, "Default"),
            FormatSelection = new FormatSelection(
                new MediaFormat { FormatId = "video", VideoCodec = "avc1", AudioCodec = "none", Height = 1080 },
                new MediaFormat { FormatId = "audio", VideoCodec = "none", AudioCodec = "aac" }),
            ExpectedDuration = TimeSpan.FromMinutes(2),
        };

    private static MediaValidationResult ValidResult(string path, bool universal) =>
        new()
        {
            FilePath = path,
            IsReadable = true,
            IsMp4 = true,
            HasVideo = true,
            HasAudio = true,
            VideoCodec = universal ? "h264" : "hevc",
            AudioCodec = "aac",
        };

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "VideoFetch.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void AssertArgumentPair(IReadOnlyList<string> arguments, string name, string value)
    {
        int index = arguments.IndexOf(name);
        Assert.IsGreaterThanOrEqualTo(0, index);
        Assert.AreEqual(value, arguments[index + 1]);
    }

    private sealed class FixedExecutableLocator : IExecutableLocator
    {
        public string? Find(string executableName, string? explicitPath = null, string? toolDirectory = null) =>
            Path.Combine("C:\\tools", executableName);
    }

    private sealed class CapturingProcessRunner(string outputPath) : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = [];

        public Task<ProcessResult> RunAsync(
            ProcessRequest request,
            IProgress<ProcessOutputLine>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            progress?.Report(new ProcessOutputLine("__VIDEOFETCH_PROGRESS__:50%|1MiB/s|00:10", false));
            return Task.FromResult(new ProcessResult(
                0,
                $"{YtDlpProgressParser.FilePrefix}{outputPath}",
                string.Empty));
        }
    }

    private sealed class QueueValidationService(params MediaValidationResult[] results) : IMediaValidationService
    {
        private readonly Queue<MediaValidationResult> results = new(results);

        public int CallCount { get; private set; }

        public Task<MediaValidationResult> ValidateAsync(
            string filePath,
            TimeSpan? expectedDuration = null,
            bool expectAudio = true,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(results.Dequeue());
        }
    }

    private sealed class CountingTranscodeService : IMediaTranscodeService
    {
        public int CallCount { get; private set; }

        public Task TranscodeToUniversalMp4Async(
            string inputPath,
            TimeSpan? duration,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}

internal static class ReadOnlyListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> values, T value)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(values[index], value))
            {
                return index;
            }
        }

        return -1;
    }
}
