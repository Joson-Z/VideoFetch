namespace VideoFetch.App.Services;

public interface IClientServiceFactory
{
    ClientServices Create(string? toolDirectory);
}
