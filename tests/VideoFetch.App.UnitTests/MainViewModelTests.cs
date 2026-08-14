using System.IO;
using VideoFetch.App.Services;
using VideoFetch.App.ViewModels;
using VideoFetch.Application.Downloads;
using VideoFetch.Application.Media;
using VideoFetch.Application.Tooling;
using VideoFetch.Domain.Authentication;
using VideoFetch.Domain.Downloads;
using VideoFetch.Domain.Media;

namespace VideoFetch.App.UnitTests;

[TestClass]
public sealed class MainViewModelTests
{
    [TestMethod]
    public async Task ProbeCommand_PopulatesVideoAndAvailableQualities()
    {
        RecordingDownloadService downloadService = new();
        MainViewModel viewModel = CreateViewModel(downloadService);
        viewModel.VideoUrl = "https://www.bilibili.com/video/BV1TEST";

        await ExecuteAsync(viewModel.ProbeCommand);

        Assert.AreEqual("测试视频", viewModel.VideoTitle);
        Assert.HasCount(3, viewModel.QualityOptions);
        Assert.AreEqual("自动：最高可用（2160P）", viewModel.QualityOptions[0].Label);
        Assert.AreEqual(2160, viewModel.QualityOptions[1].MaximumHeight);
        StringAssert.Contains(viewModel.LoginStatus, "Edge");
        Assert.AreEqual(CheckState.Success, viewModel.VideoCheckState);
        StringAssert.Contains(viewModel.VideoStatus, "检测成功");
    }

    [TestMethod]
    public async Task CheckLoginCommand_ReportsSuccessfulLoginSourceRead()
    {
        MainViewModel viewModel = CreateViewModel(new RecordingDownloadService());
        viewModel.VideoUrl = "https://www.bilibili.com/video/BV1TEST";

        await ExecuteAsync(viewModel.CheckLoginCommand);

        Assert.AreEqual(CheckState.Success, viewModel.LoginCheckState);
        StringAssert.Contains(viewModel.LoginStatus, "登录信息读取成功");
        StringAssert.Contains(viewModel.LoginStatus, "Edge");
        Assert.AreEqual("尚未解析视频", viewModel.VideoTitle);
    }

    [TestMethod]
    public async Task CheckLoginCommand_WhenProbeFails_ReportsLoginFailureSeparately()
    {
        MainViewModel viewModel = CreateViewModel(
            new RecordingDownloadService(),
            new FailingProbeService("无法读取 Chrome 登录信息：请完全退出 Chrome 后重试。"));
        viewModel.SelectedLoginMethod = viewModel.LoginMethods.Single(option => option.Value == LoginMethod.Chrome);
        viewModel.VideoUrl = "https://www.bilibili.com/video/BV1TEST";

        await ExecuteAsync(viewModel.CheckLoginCommand);

        Assert.AreEqual(CheckState.Error, viewModel.LoginCheckState);
        StringAssert.Contains(viewModel.LoginStatus, "登录检测失败");
        StringAssert.Contains(viewModel.LoginStatus, "完全退出 Chrome");
        Assert.AreEqual(CheckState.Pending, viewModel.VideoCheckState);
    }

    [TestMethod]
    public async Task DownloadCommand_UsesSelectedQualityAndOutputSettings()
    {
        RecordingDownloadService downloadService = new();
        MainViewModel viewModel = CreateViewModel(downloadService);
        viewModel.VideoUrl = "https://www.bilibili.com/video/BV1TEST";
        viewModel.OutputDirectory = Path.Combine(Path.GetTempPath(), "VideoFetch-output");
        await ExecuteAsync(viewModel.ProbeCommand);
        viewModel.SelectedQuality = viewModel.QualityOptions.Single(option => option.MaximumHeight == 1080);
        viewModel.SelectedOutputMode = viewModel.OutputModes.Single(option =>
            option.Value == Mp4OutputMode.UniversalCompatibility);

        await ExecuteAsync(viewModel.DownloadCommand);

        Assert.IsNotNull(downloadService.Request);
        Assert.AreEqual("1080", downloadService.Request.FormatSelection.Video.FormatId);
        Assert.AreEqual(Mp4OutputMode.UniversalCompatibility, downloadService.Request.OutputMode);
        Assert.AreEqual(viewModel.OutputDirectory, downloadService.Request.OutputDirectory);
        Assert.AreEqual(100, viewModel.ProgressPercent);
    }

    private static MainViewModel CreateViewModel(
        RecordingDownloadService downloadService,
        IVideoProbeService? probeService = null)
    {
        VideoMetadata metadata = new()
        {
            Id = "BV1TEST",
            Title = "测试视频",
            OriginalUrl = "https://www.bilibili.com/video/BV1TEST",
            Uploader = "测试作者",
            Duration = TimeSpan.FromMinutes(2),
            Formats =
            [
                Video("2160", 2160, 30),
                Video("1080", 1080, 60),
                Audio("audio", 192),
            ],
        };
        ClientServices services = new(
            new ReadyToolchainService(),
            new ToolchainConfiguration(),
            probeService ?? new StaticProbeService(metadata),
            new FormatSelectionService(),
            downloadService);
        return new MainViewModel(new StaticClientServiceFactory(services), new NullFileDialogService());
    }

    private static async Task ExecuteAsync(AsyncRelayCommand command)
    {
        Assert.IsTrue(command.CanExecute(null));
        command.Execute(null);
        while (command.IsRunning)
        {
            await Task.Delay(5);
        }
    }

    private static MediaFormat Video(string id, int height, double fps) =>
        new()
        {
            FormatId = id,
            Height = height,
            FramesPerSecond = fps,
            VideoCodec = "avc1",
            AudioCodec = "none",
            Extension = "mp4",
        };

    private static MediaFormat Audio(string id, double bitrate) =>
        new()
        {
            FormatId = id,
            VideoCodec = "none",
            AudioCodec = "aac",
            AudioBitrateKbps = bitrate,
            Extension = "m4a",
        };

    private sealed class StaticClientServiceFactory(ClientServices services) : IClientServiceFactory
    {
        public ClientServices Create(string? toolDirectory) => services;
    }

    private sealed class NullFileDialogService : IFileDialogService
    {
        public string? SelectCookieFile(string? currentPath) => null;

        public string? SelectFolder(string? currentPath, string title) => null;
    }

    private sealed class ReadyToolchainService : IToolchainService
    {
        public Task<ToolchainReport> CheckAsync(
            ToolchainConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            ToolCheckResult[] results =
            [
                Available(ToolComponent.YtDlp, "yt-dlp.exe"),
                Available(ToolComponent.Ffmpeg, "ffmpeg.exe"),
                Available(ToolComponent.Ffprobe, "ffprobe.exe"),
            ];
            return Task.FromResult(new ToolchainReport(results));
        }

        private static ToolCheckResult Available(ToolComponent component, string name) =>
            new(component, name, Path.Combine("C:\\tools", name), true, "1.0", null);
    }

    private sealed class StaticProbeService(VideoMetadata metadata) : IVideoProbeService
    {
        public Task<VideoMetadata> ProbeAsync(
            string url,
            LoginSource loginSource,
            CancellationToken cancellationToken = default) => Task.FromResult(metadata);
    }

    private sealed class FailingProbeService(string message) : IVideoProbeService
    {
        public Task<VideoMetadata> ProbeAsync(
            string url,
            LoginSource loginSource,
            CancellationToken cancellationToken = default) =>
            Task.FromException<VideoMetadata>(new VideoProbeException(message));
    }

    private sealed class RecordingDownloadService : IDownloadService
    {
        public DownloadRequest? Request { get; private set; }

        public Task<DownloadResult> DownloadAsync(
            DownloadRequest request,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            string path = Path.Combine(request.OutputDirectory, $"{request.Title}.mp4");
            MediaValidationResult validation = new()
            {
                FilePath = path,
                IsReadable = true,
                IsMp4 = true,
                HasVideo = true,
                HasAudio = true,
                VideoCodec = "h264",
                AudioCodec = "aac",
            };
            progress?.Report(new DownloadProgress(DownloadTaskState.Completed, 100));
            return Task.FromResult(new DownloadResult(path, validation));
        }
    }
}
