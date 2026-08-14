namespace VideoFetch.Application.Tooling;

public sealed record ToolchainReport(IReadOnlyList<ToolCheckResult> Components)
{
    public bool IsReady => Components.Count == 3 && Components.All(component => component.IsAvailable);

    public ToolCheckResult this[ToolComponent component] =>
        Components.Single(result => result.Component == component);
}
