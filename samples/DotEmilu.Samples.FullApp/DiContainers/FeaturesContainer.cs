using System.Reflection;
using FluentValidation;

namespace DotEmilu.Samples.FullApp.DiContainers;

internal static class FeaturesContainer
{
    internal static IServiceCollection AddFeatures(this IServiceCollection services)
        => services
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true)
            .AddVerifier()
            .AddHandlers(Assembly.GetExecutingAssembly());
}
