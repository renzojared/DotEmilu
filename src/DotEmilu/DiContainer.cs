using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu;

public static class DiContainer
{
    public static IServiceCollection AddDotEmilu(this IServiceCollection services)
    {
        services
            .AddOptions<ResultMessage>()
            .BindConfiguration(ResultMessage.SectionKey)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddScoped(typeof(IVerifier<>), typeof(Verifier<>))
            .AddScoped<IPresenter, Presenter>();

        return services;
    }

    public static IServiceCollection AddChainHandlers(this IServiceCollection services, Assembly assembly)
    {
        var assemblies = assembly
            .GetTypes()
            .Where(s => s is { IsClass: true, IsAbstract: false, BaseType.IsGenericType: true } &&
                        s.BaseType.GetGenericTypeDefinition() == typeof(ChainHandler<>))
            .ToList();

        assemblies.ForEach(handler => services.AddScoped(handler));

        return services;
    }
}