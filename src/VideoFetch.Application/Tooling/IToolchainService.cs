namespace VideoFetch.Application.Tooling;

public interface IToolchainService
{
    Task<ToolchainReport> CheckAsync(
        ToolchainConfiguration configuration,
        CancellationToken cancellationToken = default);
}
