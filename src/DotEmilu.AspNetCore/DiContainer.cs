using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotEmilu.AspNetCore;

/// <summary>
/// Provides extension methods for configuring dependency injection for ASP.NET Core integration.
/// </summary>
public static class DiContainer
{
    /// <summary>Registers the necessary DotEmilu ASP.NET Core services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDotEmilu(this IServiceCollection services)
        => services
            .AddVerifier()
            .AddHttpHandler();

    private static IServiceCollection AddHttpHandler(this IServiceCollection services)
    {
        services
            .AddOptions<ResultMessage>()
            .BindConfiguration(ResultMessage.SectionKey)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddScoped<IPresenter, Presenter>();
        services.TryAddScoped(typeof(HttpHandler<>));
        services.TryAddScoped(typeof(HttpHandler<,>));

        return services;
    }
}