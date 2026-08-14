namespace VideoFetch.Application.Tooling;

public sealed record ToolCheckResult(
    ToolComponent Component,
    string ExecutableName,
    string? ResolvedPath,
    bool IsAvailable,
    string? Version,
    string? Error);
