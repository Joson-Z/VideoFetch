using VideoFetch.Domain.Media;

namespace VideoFetch.Application.Media;

public sealed class FormatSelectionService : IFormatSelectionService
{
    public FormatSelection Select(
        IReadOnlyCollection<MediaFormat> formats,
        FormatSelectionPreference preference)
    {
        ArgumentNullException.ThrowIfNull(formats);
        ArgumentNullException.ThrowIfNull(preference);

        if (preference.MaximumHeight is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(preference), "画质上限必须大于 0。");
        }

        List<MediaFormat> videoCandidates = formats
            .Where(format => format.HasVideo)
            .Where(format => preference.MaximumHeight is null || format.Height is null || format.Height <= preference.MaximumHeight)
            .ToList();

        if (videoCandidates.Count == 0)
        {
            throw new FormatSelectionException("当前视频没有符合条件的视频格式。");
        }

        MediaFormat selectedVideo = SelectBestVideo(videoCandidates, preference.OutputMode);
        if (selectedVideo.HasAudio)
        {
            return new FormatSelection(selectedVideo, null);
        }

        MediaFormat? selectedAudio = formats
            .Where(format => format.IsAudioOnly)
            .OrderByDescending(format => format.AudioBitrateKbps ?? format.TotalBitrateKbps ?? 0)
            .ThenByDescending(format => format.FileSizeBytes ?? 0)
            .FirstOrDefault();

        if (selectedAudio is not null)
        {
            return new FormatSelection(selectedVideo, selectedAudio);
        }

        MediaFormat? combinedFallback = videoCandidates
            .Where(format => format.HasAudio)
            .OrderByDescending(format => format.Height ?? 0)
            .ThenByDescending(format => format.FramesPerSecond ?? 0)
            .ThenByDescending(format => format.TotalBitrateKbps ?? 0)
            .FirstOrDefault();

        return combinedFallback is null
            ? new FormatSelection(selectedVideo, null)
            : new FormatSelection(combinedFallback, null);
    }

    private static MediaFormat SelectBestVideo(
        IReadOnlyCollection<MediaFormat> candidates,
        Mp4OutputMode outputMode)
    {
        int bestHeight = candidates.Max(format => format.Height ?? 0);
        IEnumerable<MediaFormat> bestResolution = candidates.Where(format => (format.Height ?? 0) == bestHeight);
        double bestFrameRate = bestResolution.Max(format => format.FramesPerSecond ?? 0);
        IEnumerable<MediaFormat> bestFrameRateFormats = bestResolution
            .Where(format => (format.FramesPerSecond ?? 0).Equals(bestFrameRate));

        return bestFrameRateFormats
            .OrderByDescending(format => outputMode == Mp4OutputMode.UniversalCompatibility && IsAvcMp4(format))
            .ThenByDescending(format => HasHighDynamicRange(format.DynamicRange))
            .ThenByDescending(format => format.VideoBitrateKbps ?? format.TotalBitrateKbps ?? 0)
            .ThenByDescending(format => format.Width ?? 0)
            .ThenByDescending(format => format.FileSizeBytes ?? 0)
            .First();
    }

    private static bool IsAvcMp4(MediaFormat format) =>
        string.Equals(format.Extension, "mp4", StringComparison.OrdinalIgnoreCase)
        && format.VideoCodec is { } codec
        && (codec.StartsWith("avc", StringComparison.OrdinalIgnoreCase)
            || codec.StartsWith("h264", StringComparison.OrdinalIgnoreCase));

    private static bool HasHighDynamicRange(string? dynamicRange) =>
        dynamicRange is not null
        && !string.Equals(dynamicRange, "SDR", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(dynamicRange, "SDR TV", StringComparison.OrdinalIgnoreCase);
}
