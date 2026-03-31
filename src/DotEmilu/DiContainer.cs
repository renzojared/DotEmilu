using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotEmilu;

/// <summary>
/// Provides extension methods for configuring dependency injection.
/// </summary>
public static class DiContainer
{
    /// <summary>Registers the default verifier implementation.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddVerifier(this IServiceCollection services)
    {
        services.TryAddScoped(typeof(IVerifier<>), typeof(Verifier<>));
        return services;
    }

    /// <summary>Registers all handlers found in the specified assembly.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var implementations = GetHandlerImplementations(assembly);

        foreach (var implementation in implementations)
        {
            var interfaceHandler = implementation.GetInterfaces().First(IsHandlerInterface);
            services.TryAddScoped(interfaceHandler, implementation);
        }

        return services;

        static IEnumerable<Type> GetHandlerImplementations(Assembly assembly)
            => assembly
                .GetTypes()
                .Where(i => i is { IsAbstract: false, IsInterface: false } &&
                            i.GetInterfaces().Any(IsHandlerInterface));

        static bool IsHandlerInterface(Type i)
            => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IHandler<>) ||
                                   i.GetGenericTypeDefinition() == typeof(IHandler<,>));
    }

    /// <summary>Registers all chain handlers found in the specified assembly.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for chain handlers.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddChainHandlers(this IServiceCollection services, Assembly assembly)
    {
        var chainHandlers = assembly
            .GetTypes()
            .Where(s => s is { IsClass: true, IsAbstract: false, BaseType.IsGenericType: true } &&
                        s.BaseType.GetGenericTypeDefinition() == typeof(ChainHandler<>));

        foreach (var chainHandler in chainHandlers)
        {
            services.TryAddScoped(chainHandler);
        }

        return services;
    }
}