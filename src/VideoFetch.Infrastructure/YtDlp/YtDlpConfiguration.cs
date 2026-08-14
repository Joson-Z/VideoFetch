namespace VideoFetch.Infrastructure.YtDlp;

public sealed record YtDlpConfiguration
{
    public string? ExecutablePath { get; init; }

    public string? ToolDirectory { get; init; }
}
