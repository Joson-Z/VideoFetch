using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;

namespace VideoFetch.UnitTests.Tooling;

[TestClass]
public sealed class ToolchainServiceTests
{
    [TestMethod]
    public async Task CheckAsync_WhenAllToolsRespond_ReturnsReadyReport()
    {
        FakeExecutableLocator locator = new(found: true);
        FakeProcessRunner runner = new(request =>
            new ProcessResult(0, $"{Path.GetFileName(request.FileName)} 1.0", string.Empty));
        ToolchainService service = new(locator, runner);

        ToolchainReport report = await service.CheckAsync(new ToolchainConfiguration());

        Assert.IsTrue(report.IsReady);
        Assert.HasCount(3, report.Components);
        Assert.HasCount(3, runner.Requests);
        Assert.IsTrue(report[ToolComponent.YtDlp].Version!.Contains("yt-dlp.exe", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CheckAsync_WhenToolsAreMissing_DoesNotStartProcesses()
    {
        FakeExecutableLocator locator = new(found: false);
        FakeProcessRunner runner = new(_ => throw new AssertFailedException("不应启动进程"));
        ToolchainService service = new(locator, runner);

        ToolchainReport report = await service.CheckAsync(new ToolchainConfiguration());

        Assert.IsFalse(report.IsReady);
        Assert.IsTrue(report.Components.All(component => !component.IsAvailable));
        Assert.IsEmpty(runner.Requests);
    }

    [TestMethod]
    public async Task CheckAsync_WhenVersionCommandFails_ReturnsError()
    {
        FakeExecutableLocator locator = new(found: true);
        FakeProcessRunner runner = new(_ => new ProcessResult(2, string.Empty, "bad executable"));
        ToolchainService service = new(locator, runner);

        ToolchainReport report = await service.CheckAsync(new ToolchainConfiguration());

        Assert.IsFalse(report.IsReady);
        Assert.IsTrue(report.Components.All(component => component.Error == "bad executable"));
    }

    private sealed class FakeExecutableLocator(bool found) : IExecutableLocator
    {
        public string? Find(string executableName, string? explicitPath = null, string? toolDirectory = null) =>
            found ? Path.Combine("C:\\tools", executableName) : null;
    }

    private sealed class FakeProcessRunner(Func<ProcessRequest, ProcessResult> resultFactory) : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = [];

        public Task<ProcessResult> RunAsync(
            ProcessRequest request,
            IProgress<ProcessOutputLine>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(resultFactory(request));
        }
    }
}
