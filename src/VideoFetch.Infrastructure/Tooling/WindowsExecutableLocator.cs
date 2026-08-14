using VideoFetch.Application.Tooling;

namespace VideoFetch.Infrastructure.Tooling;

public sealed class WindowsExecutableLocator : IExecutableLocator
{
    public string? Find(string executableName, string? explicitPath = null, string? toolDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableName);

        string? explicitResult = ResolveFile(explicitPath);
        if (explicitResult is not null)
        {
            return explicitResult;
        }

        if (!string.IsNullOrWhiteSpace(toolDirectory))
        {
            string? directoryResult = ResolveFile(Path.Combine(toolDirectory, executableName));
            if (directoryResult is not null)
            {
                return directoryResult;
            }
        }

        string bundledPath = Path.Combine(AppContext.BaseDirectory, "tools", executableName);
        string? bundledResult = ResolveFile(bundledPath);
        if (bundledResult is not null)
        {
            return bundledResult;
        }

        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (string pathEntry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string? pathResult = ResolveFile(Path.Combine(pathEntry.Trim(), executableName));
            if (pathResult is not null)
            {
                return pathResult;
            }
        }

        return null;
    }

    private static string? ResolveFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        return File.Exists(fullPath) ? fullPath : null;
    }
}
