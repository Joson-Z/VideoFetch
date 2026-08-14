using VideoFetch.Application.Media;
using VideoFetch.Application.Processes;
using VideoFetch.Application.Tooling;
using VideoFetch.Domain.Authentication;
using VideoFetch.Domain.Media;
using VideoFetch.Infrastructure.YtDlp;

namespace VideoFetch.UnitTests.YtDlp;

[TestClass]
public sealed class YtDlpVideoProbeServiceTests
{
    private const string VideoUrl = "https://www.bilibili.com/video/BV1TEST";

    [TestMethod]
    public async Task ProbeAsync_WithBrowserLogin_PassesProfileAndUrlAsSeparateArguments()
    {
        CapturingProcessRunner runner = new(SuccessResult());
        YtDlpVideoProbeService service = CreateService(runner);

        VideoMetadata metadata = await service.ProbeAsync(
            VideoUrl,
            new LoginSource.Browser(BrowserType.Edge, "Default"));

        Assert.AreEqual("测试视频", metadata.Title);
        ProcessRequest request = AssertExactlyOne(runner.Requests);
        CollectionAssert.Contains(request.Arguments.ToArray(), "--cookies-from-browser");
        CollectionAssert.Contains(request.Arguments.ToArray(), "edge:Default");
        Assert.AreEqual("--", request.Arguments[^2]);
        Assert.AreEqual(VideoUrl, request.Arguments[^1]);
    }

    [TestMethod]
    public async Task ProbeAsync_WithAnonymousLogin_DoesNotAddCookieArguments()
    {
        CapturingProcessRunner runner = new(SuccessResult());
        YtDlpVideoProbeService service = CreateService(runner);

        await service.ProbeAsync(VideoUrl, new LoginSource.Anonymous());

        ProcessRequest request = AssertExactlyOne(runner.Requests);
        CollectionAssert.DoesNotContain(request.Arguments.ToArray(), "--cookies");
        CollectionAssert.DoesNotContain(request.Arguments.ToArray(), "--cookies-from-browser");
    }

    [TestMethod]
    public async Task ProbeAsync_WithMissingCookieFile_DoesNotStartProcess()
    {
        CapturingProcessRunner runner = new(SuccessResult());
        YtDlpVideoProbeService service = CreateService(runner);

        await Assert.ThrowsExactlyAsync<VideoProbeException>(() =>
            service.ProbeAsync(VideoUrl, new LoginSource.CookieFile(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.txt"))));

        Assert.IsEmpty(runner.Requests);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenUrlIsUnsupported_DoesNotStartProcess()
    {
        CapturingProcessRunner runner = new(SuccessResult());
        YtDlpVideoProbeService service = CreateService(runner);

        await Assert.ThrowsExactlyAsync<VideoProbeException>(() =>
            service.ProbeAsync("https://example.com/video", new LoginSource.Anonymous()));

        Assert.IsEmpty(runner.Requests);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenYtDlpFails_ReturnsConciseError()
    {
        CapturingProcessRunner runner = new(new ProcessResult(1, string.Empty, "ERROR: 登录状态已失效\r\nDebug details"));
        YtDlpVideoProbeService service = CreateService(runner);

        VideoProbeException exception = await Assert.ThrowsExactlyAsync<VideoProbeException>(() =>
            service.ProbeAsync(VideoUrl, new LoginSource.Anonymous()));

        Assert.AreEqual("ERROR: 登录状态已失效", exception.Message);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenChromeCookieDatabaseIsLocked_ReturnsActionableChineseError()
    {
        CapturingProcessRunner runner = new(new ProcessResult(
            1,
            string.Empty,
            "ERROR: Could not copy Chrome cookie database. See https://github.com/yt-dlp/yt-dlp/issues/7271 for more info"));
        YtDlpVideoProbeService service = CreateService(runner);

        VideoProbeException exception = await Assert.ThrowsExactlyAsync<VideoProbeException>(() =>
            service.ProbeAsync(VideoUrl, new LoginSource.Browser(BrowserType.Chrome, "Default")));

        StringAssert.Contains(exception.Message, "完全退出 Chrome");
        StringAssert.Contains(exception.Message, "cookies.txt");
        Assert.DoesNotContain("github.com", exception.Message);
    }

    private static YtDlpVideoProbeService CreateService(IProcessRunner runner) =>
        new(
            new YtDlpConfiguration(),
            new FixedExecutableLocator(),
            runner,
            new BilibiliUrlValidator(),
            new YtDlpMetadataParser());

    private static ProcessResult SuccessResult() =>
        new(0, "{\"id\":\"BV1TEST\",\"title\":\"测试视频\",\"formats\":[]}", string.Empty);

    private static T AssertExactlyOne<T>(IReadOnlyCollection<T> values)
    {
        Assert.HasCount(1, values);
        return values.Single();
    }

    private sealed class FixedExecutableLocator : IExecutableLocator
    {
        public string? Find(string executableName, string? explicitPath = null, string? toolDirectory = null) =>
            Path.Combine("C:\\tools", executableName);
    }

    private sealed class CapturingProcessRunner(ProcessResult result) : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = [];

        public Task<ProcessResult> RunAsync(
            ProcessRequest request,
            IProgress<ProcessOutputLine>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(result);
        }
    }
}
