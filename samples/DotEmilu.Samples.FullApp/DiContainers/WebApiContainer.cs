using DotEmilu.AspNetCore;

namespace DotEmilu.Samples.FullApp.DiContainers;

internal static class WebApiContainer
{
    internal static IServiceCollection AddWebApi(this IServiceCollection services)
        => services
            .AddDotEmilu()
            .AddOpenApi();
}
