namespace VideoFetch.Application.Tooling;

public sealed record ToolchainConfiguration
{
    public string? ToolDirectory { get; init; }

    public string? YtDlpPath { get; init; }

    public string? FfmpegPath { get; init; }

    public string? FfprobePath { get; init; }
}
