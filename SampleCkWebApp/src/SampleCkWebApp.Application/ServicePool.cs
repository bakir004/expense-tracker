using Microsoft.Extensions.DependencyInjection;

namespace SampleCkWebApp.Application;

public class ServicePool
{
    private static ServicePool? _instance;

    private readonly IServiceProvider _provider;

    private ServicePool(IServiceProvider provider)
    {
        _provider = provider;
    }

    public static void Create(IServiceProvider serviceProvider)
    {
        _instance = new ServicePool(serviceProvider);
    }

    public static TService? GetService<TService>()
    {
        return _instance!._provider.GetService<TService>();
    }

    public static TService GetRequiredService<TService>()
        where TService : notnull
    {
        return _instance!._provider.GetRequiredService<TService>();
    }
}
