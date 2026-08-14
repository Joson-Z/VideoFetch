using System.Collections.ObjectModel;
using System.IO;
using VideoFetch.App.Services;
using VideoFetch.Application.Downloads;
using VideoFetch.Application.Media;
using VideoFetch.Application.Tooling;
using VideoFetch.Domain.Authentication;
using VideoFetch.Domain.Downloads;
using VideoFetch.Domain.Media;

namespace VideoFetch.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IClientServiceFactory serviceFactory;
    private readonly IFileDialogService fileDialogService;
    private VideoMetadata? metadata;
    private string videoUrl = string.Empty;
    private LoginMethodOption selectedLoginMethod;
    private string browserProfile = "Default";
    private string cookieFilePath = string.Empty;
    private string toolDirectory;
    private string outputDirectory;
    private QualityOption? selectedQuality;
    private OutputModeOption selectedOutputMode;
    private string videoTitle = "尚未解析视频";
    private string videoDetails = "输入 B 站视频链接后检测登录状态并读取可用画质。";
    private string toolStatus = "尚未检测 yt-dlp 和 FFmpeg";
    private string loginStatus = "尚未检测";
    private string statusMessage = "准备就绪";
    private string progressDetail = string.Empty;
    private double progressPercent;
    private bool isBusy;
    private string? completedFilePath;

    public MainViewModel(IClientServiceFactory serviceFactory, IFileDialogService fileDialogService)
    {
        this.serviceFactory = serviceFactory;
        this.fileDialogService = fileDialogService;

        LoginMethods =
        [
            new LoginMethodOption(LoginMethod.Edge, "Microsoft Edge 登录态"),
            new LoginMethodOption(LoginMethod.Chrome, "Google Chrome 登录态"),
            new LoginMethodOption(LoginMethod.CookieFile, "导入 cookies.txt"),
            new LoginMethodOption(LoginMethod.Anonymous, "不登录（公开画质）"),
        ];
        selectedLoginMethod = LoginMethods[0];

        OutputModes =
        [
            new OutputModeOption(
                Mp4OutputMode.PreserveSourceQuality,
                "保留最高源质量",
                "优先转封装，不重新编码；老旧播放器可能不支持 HEVC/AV1。"),
            new OutputModeOption(
                Mp4OutputMode.UniversalCompatibility,
                "通用兼容 MP4",
                "必要时转码为 H.264/AAC，兼容性更好但耗时更长。"),
        ];
        selectedOutputMode = OutputModes[0];

        toolDirectory = Path.Combine(AppContext.BaseDirectory, "tools");
        outputDirectory = GetDefaultOutputDirectory();
        QualityOptions.Add(new QualityOption(null, "自动：最高可用"));
        selectedQuality = QualityOptions[0];

        CheckToolsCommand = new AsyncRelayCommand(CheckToolsAsync, () => !IsBusy, HandleUnexpectedError);
        ProbeCommand = new AsyncRelayCommand(ProbeAsync, CanProbe, HandleUnexpectedError);
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, CanDownload, HandleUnexpectedError);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        SelectCookieFileCommand = new RelayCommand(SelectCookieFile, () => !IsBusy);
        SelectToolDirectoryCommand = new RelayCommand(SelectToolDirectory, () => !IsBusy);
        SelectOutputDirectoryCommand = new RelayCommand(SelectOutputDirectory, () => !IsBusy);
    }

    public IReadOnlyList<LoginMethodOption> LoginMethods { get; }

    public IReadOnlyList<OutputModeOption> OutputModes { get; }

    public ObservableCollection<QualityOption> QualityOptions { get; } = [];

    public AsyncRelayCommand CheckToolsCommand { get; }

    public AsyncRelayCommand ProbeCommand { get; }

    public AsyncRelayCommand DownloadCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand SelectCookieFileCommand { get; }

    public RelayCommand SelectToolDirectoryCommand { get; }

    public RelayCommand SelectOutputDirectoryCommand { get; }

    public string VideoUrl
    {
        get => videoUrl;
        set
        {
            if (SetProperty(ref videoUrl, value))
            {
                metadata = null;
                CompletedFilePath = null;
                VideoTitle = "尚未解析视频";
                VideoDetails = "链接已变化，请重新解析。";
                RefreshCommandStates();
            }
        }
    }

    public LoginMethodOption SelectedLoginMethod
    {
        get => selectedLoginMethod;
        set
        {
            if (SetProperty(ref selectedLoginMethod, value))
            {
                LoginStatus = "登录来源已变化，请重新解析";
                metadata = null;
                OnPropertyChanged(nameof(IsBrowserLogin));
                OnPropertyChanged(nameof(IsCookieFileLogin));
                RefreshCommandStates();
            }
        }
    }

    public bool IsBrowserLogin => SelectedLoginMethod.Value is LoginMethod.Edge or LoginMethod.Chrome;

    public bool IsCookieFileLogin => SelectedLoginMethod.Value == LoginMethod.CookieFile;

    public string BrowserProfile
    {
        get => browserProfile;
        set => SetProperty(ref browserProfile, value);
    }

    public string CookieFilePath
    {
        get => cookieFilePath;
        set => SetProperty(ref cookieFilePath, value);
    }

    public string ToolDirectory
    {
        get => toolDirectory;
        set
        {
            if (SetProperty(ref toolDirectory, value))
            {
                ToolStatus = "工具目录已变化，请重新检测";
            }
        }
    }

    public string OutputDirectory
    {
        get => outputDirectory;
        set => SetProperty(ref outputDirectory, value);
    }

    public QualityOption? SelectedQuality
    {
        get => selectedQuality;
        set => SetProperty(ref selectedQuality, value);
    }

    public OutputModeOption SelectedOutputMode
    {
        get => selectedOutputMode;
        set => SetProperty(ref selectedOutputMode, value);
    }

    public string VideoTitle
    {
        get => videoTitle;
        private set => SetProperty(ref videoTitle, value);
    }

    public string VideoDetails
    {
        get => videoDetails;
        private set => SetProperty(ref videoDetails, value);
    }

    public string ToolStatus
    {
        get => toolStatus;
        private set => SetProperty(ref toolStatus, value);
    }

    public string LoginStatus
    {
        get => loginStatus;
        private set => SetProperty(ref loginStatus, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string ProgressDetail
    {
        get => progressDetail;
        private set => SetProperty(ref progressDetail, value);
    }

    public double ProgressPercent
    {
        get => progressPercent;
        private set => SetProperty(ref progressPercent, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(IsIdle));
                RefreshCommandStates();
            }
        }
    }

    public bool IsIdle => !IsBusy;

    public string? CompletedFilePath
    {
        get => completedFilePath;
        private set => SetProperty(ref completedFilePath, value);
    }

    public async Task InitializeAsync() => await CheckToolsAsync(CancellationToken.None);

    private async Task CheckToolsAsync(CancellationToken cancellationToken)
    {
        await RunBusyAsync(async () =>
        {
            StatusMessage = "正在检测工具链…";
            ClientServices services = serviceFactory.Create(ToolDirectory);
            ToolchainReport report = await services.ToolchainService.CheckAsync(
                services.ToolchainConfiguration,
                cancellationToken);
            ToolStatus = string.Join("  ·  ", report.Components.Select(component =>
                component.IsAvailable
                    ? $"{component.ExecutableName}: {ShortenVersion(component.Version)}"
                    : $"{component.ExecutableName}: 缺失"));
            StatusMessage = report.IsReady
                ? "工具链检测通过"
                : "工具不完整：请将 yt-dlp.exe、ffmpeg.exe、ffprobe.exe 放入工具目录";
        });
    }

    private async Task ProbeAsync(CancellationToken cancellationToken)
    {
        await RunBusyAsync(async () =>
        {
            StatusMessage = "正在检测登录状态并解析视频…";
            ProgressPercent = 0;
            ClientServices services = serviceFactory.Create(ToolDirectory);
            ToolchainReport report = await services.ToolchainService.CheckAsync(
                services.ToolchainConfiguration,
                cancellationToken);
            if (!report.IsReady)
            {
                throw new InvalidOperationException("工具链不完整，请先配置并检测工具目录。");
            }

            LoginSource loginSource = BuildLoginSource();
            VideoMetadata result = await services.VideoProbeService.ProbeAsync(
                VideoUrl.Trim(),
                loginSource,
                cancellationToken);
            metadata = result;
            VideoTitle = result.Title;
            VideoDetails = BuildVideoDetails(result);
            LoginStatus = SelectedLoginMethod.Value switch
            {
                LoginMethod.Edge => "已使用 Edge 登录态读取可用格式",
                LoginMethod.Chrome => "已使用 Chrome 登录态读取可用格式",
                LoginMethod.CookieFile => "已使用 Cookie 文件读取可用格式",
                _ => "未登录；当前显示公开可用格式",
            };
            PopulateQualityOptions(result.Formats);
            StatusMessage = $"解析完成：发现 {result.Formats.Count} 个媒体格式";
        });
    }

    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        await RunBusyAsync(async () =>
        {
            if (metadata is null || SelectedQuality is null)
            {
                throw new InvalidOperationException("请先解析视频并选择画质。");
            }

            CompletedFilePath = null;
            ProgressPercent = 0;
            ProgressDetail = string.Empty;
            ClientServices services = serviceFactory.Create(ToolDirectory);
            FormatSelection selection = services.FormatSelectionService.Select(
                metadata.Formats,
                new FormatSelectionPreference
                {
                    MaximumHeight = SelectedQuality.MaximumHeight,
                    OutputMode = SelectedOutputMode.Value,
                });
            Progress<DownloadProgress> progress = new(UpdateDownloadProgress);
            DownloadResult result = await services.DownloadService.DownloadAsync(
                new DownloadRequest
                {
                    Url = VideoUrl.Trim(),
                    Title = metadata.Title,
                    OutputDirectory = OutputDirectory,
                    LoginSource = BuildLoginSource(),
                    FormatSelection = selection,
                    OutputMode = SelectedOutputMode.Value,
                    ExpectedDuration = metadata.Duration,
                },
                progress,
                cancellationToken);
            CompletedFilePath = result.OutputPath;
            ProgressPercent = 100;
            ProgressDetail = result.OutputPath;
            StatusMessage = "下载、合并与校验全部完成";
        });
    }

    private async Task RunBusyAsync(Func<Task> operation)
    {
        IsBusy = true;
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "任务已取消";
            ProgressDetail = string.Empty;
            throw;
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateDownloadProgress(DownloadProgress progress)
    {
        ProgressPercent = progress.Percent ?? ProgressPercent;
        string stage = progress.State switch
        {
            DownloadTaskState.Downloading => "下载中",
            DownloadTaskState.Merging => "合并中",
            DownloadTaskState.Remuxing => "转封装中",
            DownloadTaskState.Transcoding => "转码中",
            DownloadTaskState.Validating => "校验中",
            DownloadTaskState.Completed => "已完成",
            _ => progress.State.ToString(),
        };
        string[] details = [stage, progress.Speed ?? string.Empty, progress.EstimatedTimeRemaining is null ? string.Empty : $"剩余 {progress.EstimatedTimeRemaining}", progress.Message ?? string.Empty];
        ProgressDetail = string.Join("  ·  ", details.Where(value => value.Length > 0));
    }

    private LoginSource BuildLoginSource() => SelectedLoginMethod.Value switch
    {
        LoginMethod.Edge => new LoginSource.Browser(BrowserType.Edge, NormalizeOptional(BrowserProfile)),
        LoginMethod.Chrome => new LoginSource.Browser(BrowserType.Chrome, NormalizeOptional(BrowserProfile)),
        LoginMethod.CookieFile when !string.IsNullOrWhiteSpace(CookieFilePath) => new LoginSource.CookieFile(CookieFilePath.Trim()),
        LoginMethod.CookieFile => throw new InvalidOperationException("请选择 Cookie 文件。"),
        _ => new LoginSource.Anonymous(),
    };

    private void PopulateQualityOptions(IEnumerable<MediaFormat> formats)
    {
        int[] heights = formats
            .Where(format => format.HasVideo && format.Height is > 0)
            .Select(format => format.Height!.Value)
            .Distinct()
            .OrderDescending()
            .ToArray();
        QualityOptions.Clear();
        string automaticLabel = heights.Length == 0
            ? "自动：最高可用"
            : $"自动：最高可用（{heights[0]}P）";
        QualityOptions.Add(new QualityOption(null, automaticLabel));
        foreach (int height in heights)
        {
            bool highFrameRate = formats.Any(format => format.Height == height && format.FramesPerSecond >= 50);
            QualityOptions.Add(new QualityOption(height, highFrameRate ? $"{height}P（含高帧率）" : $"{height}P"));
        }

        SelectedQuality = QualityOptions[0];
    }

    private void SelectCookieFile()
    {
        if (fileDialogService.SelectCookieFile(CookieFilePath) is { } selected)
        {
            CookieFilePath = selected;
        }
    }

    private void SelectToolDirectory()
    {
        if (fileDialogService.SelectFolder(ToolDirectory, "选择 yt-dlp 与 FFmpeg 所在目录") is { } selected)
        {
            ToolDirectory = selected;
        }
    }

    private void SelectOutputDirectory()
    {
        if (fileDialogService.SelectFolder(OutputDirectory, "选择视频保存目录") is { } selected)
        {
            OutputDirectory = selected;
        }
    }

    private void Cancel()
    {
        CheckToolsCommand.Cancel();
        ProbeCommand.Cancel();
        DownloadCommand.Cancel();
    }

    private bool CanProbe() => !IsBusy && !string.IsNullOrWhiteSpace(VideoUrl);

    private bool CanDownload() => !IsBusy && metadata is not null && SelectedQuality is not null;

    private void RefreshCommandStates()
    {
        CheckToolsCommand.NotifyCanExecuteChanged();
        ProbeCommand.NotifyCanExecuteChanged();
        DownloadCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        SelectCookieFileCommand.NotifyCanExecuteChanged();
        SelectToolDirectoryCommand.NotifyCanExecuteChanged();
        SelectOutputDirectoryCommand.NotifyCanExecuteChanged();
    }

    private void HandleUnexpectedError(Exception exception) => StatusMessage = exception.Message;

    private static string BuildVideoDetails(VideoMetadata value)
    {
        string uploader = string.IsNullOrWhiteSpace(value.Uploader) ? "未知作者" : value.Uploader;
        string duration = value.Duration is null ? "时长未知" : value.Duration.Value.ToString(@"hh\:mm\:ss");
        return $"{uploader}  ·  {duration}  ·  {value.Formats.Count} 个可用格式";
    }

    private static string ShortenVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "可用";
        }

        return version.Length <= 28 ? version : $"{version[..28]}…";
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetDefaultOutputDirectory()
    {
        string videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        string root = string.IsNullOrWhiteSpace(videos)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : videos;
        return Path.Combine(root, "VideoFetch");
    }
}
