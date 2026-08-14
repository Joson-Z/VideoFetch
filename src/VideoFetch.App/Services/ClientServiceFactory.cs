using System.IO;
using VideoFetch.Application.Media;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Infrastructure.Ffmpeg;
using VideoFetch.Infrastructure.Files;
using VideoFetch.Infrastructure.Processes;
using VideoFetch.Infrastructure.Tooling;
using VideoFetch.Infrastructure.YtDlp;

namespace VideoFetch.App.Services;

public sealed class ClientServiceFactory : IClientServiceFactory
{
    public ClientServices Create(string? toolDirectory)
    {
        ToolchainConfiguration toolchainConfiguration = new()
        {
            ToolDirectory = NormalizeOptionalPath(toolDirectory),
        };
        YtDlpConfiguration ytDlpConfiguration = new()
        {
            ToolDirectory = toolchainConfiguration.ToolDirectory,
        };

        IExecutableLocator executableLocator = new WindowsExecutableLocator();
        IProcessRunner processRunner = new SystemProcessRunner();
        ToolchainService toolchainService = new(executableLocator, processRunner);
        YtDlpMetadataParser metadataParser = new();
        BilibiliUrlValidator urlValidator = new();
        YtDlpVideoProbeService videoProbeService = new(
            ytDlpConfiguration,
            executableLocator,
            processRunner,
            urlValidator,
            metadataParser);
        FfprobeMediaValidationService validationService = new(
            toolchainConfiguration,
            executableLocator,
            processRunner);
        FfmpegTranscodeService transcodeService = new(
            toolchainConfiguration,
            executableLocator,
            processRunner);
        YtDlpDownloadService downloadService = new(
            ytDlpConfiguration,
            executableLocator,
            processRunner,
            new WindowsFileNameService(),
            validationService,
            transcodeService,
            new YtDlpProgressParser());

        return new ClientServices(
            toolchainService,
            toolchainConfiguration,
            videoProbeService,
            new FormatSelectionService(),
            downloadService);
    }

    private static string? NormalizeOptionalPath(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path.Trim());
}
