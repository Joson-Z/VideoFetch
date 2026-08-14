namespace VideoFetch.Application.Tooling;

public interface IExecutableLocator
{
    string? Find(string executableName, string? explicitPath = null, string? toolDirectory = null);
}
