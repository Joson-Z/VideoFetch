using VideoFetch.Application;
using VideoFetch.Domain;
using VideoFetch.Infrastructure;

namespace VideoFetch.UnitTests;

[TestClass]
public sealed class SolutionSmokeTests
{
    [TestMethod]
    public void LayerAssemblies_AreLoadable()
    {
        Assert.AreEqual("VideoFetch.Domain", typeof(DomainAssemblyMarker).Assembly.GetName().Name);
        Assert.AreEqual("VideoFetch.Application", typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
        Assert.AreEqual("VideoFetch.Infrastructure", typeof(InfrastructureAssemblyMarker).Assembly.GetName().Name);
    }
}
