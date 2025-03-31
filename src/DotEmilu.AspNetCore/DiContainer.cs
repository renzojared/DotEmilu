using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotEmilu.AspNetCore;

public static class DiContainer
{
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