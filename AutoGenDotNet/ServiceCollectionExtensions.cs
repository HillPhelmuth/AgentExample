using AutoGenDotNet.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoGenDotNet;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add AutoGen Agent service to DI container
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddAutoGenService(this IServiceCollection services)
    {
        return services.AddScoped<AutoGenService>()./*AddScoped<BingWebSearchService>().*/AddSingleton<StringEventWriter>();
    }
    public static IServiceCollection AddBing(this IServiceCollection services)
    {
        return services.AddScoped<BingWebSearchService>();
    }
}