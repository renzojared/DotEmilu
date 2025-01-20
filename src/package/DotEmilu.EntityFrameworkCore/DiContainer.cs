using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.EntityFrameworkCore;

public static class DiContainer
{
    public static IServiceCollection AddDotEmiluInterceptors<TContextUser>(this IServiceCollection services)
        where TContextUser : class, IContextUser
        => services
            .AddSingleton(TimeProvider.System)
            .AddScoped<IContextUser, TContextUser>()
            .AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
}