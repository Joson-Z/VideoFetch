using VideoFetch.Domain.Media;

namespace VideoFetch.App.ViewModels;

public sealed record LoginMethodOption(LoginMethod Value, string Label);

public sealed record QualityOption(int? MaximumHeight, string Label);

public sealed record OutputModeOption(Mp4OutputMode Value, string Label, string Description);
