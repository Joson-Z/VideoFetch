namespace VideoFetch.Infrastructure.Processes;

internal sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
{
    public void Report(T value) => handler(value);
}
