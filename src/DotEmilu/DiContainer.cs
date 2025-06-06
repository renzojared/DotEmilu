using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotEmilu;

public static class DiContainer
{
    public static IServiceCollection AddVerifier(this IServiceCollection services)
    {
        services.TryAddScoped(typeof(IVerifier<>), typeof(Verifier<>));
        return services;
    }

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