using VideoFetch.Application.Downloads;
using VideoFetch.Application.Media;
using VideoFetch.Application.Tooling;

namespace VideoFetch.App.Services;

public sealed record ClientServices(
    IToolchainService ToolchainService,
    ToolchainConfiguration ToolchainConfiguration,
    IVideoProbeService VideoProbeService,
    IFormatSelectionService FormatSelectionService,
    IDownloadService DownloadService);
