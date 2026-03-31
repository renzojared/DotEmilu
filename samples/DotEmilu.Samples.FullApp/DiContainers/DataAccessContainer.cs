using System.Reflection;
using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.FullApp.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DotEmilu.Samples.FullApp.DiContainers;

internal static class DataAccessContainer
{
    internal static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services
            .AddSoftDeleteInterceptor()
            .AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly())
            .AddHttpContextAccessor()
            .AddScoped<Domain.Contracts.IAppUserContext, CurrentUser>()
            .AddScoped<Abstractions.IContextUser<Guid>>(sp =>
                sp.GetRequiredService<Domain.Contracts.IAppUserContext>());

        services.AddDbContext<InvoiceDbContext>((sp, options) =>
            options
                .UseInMemoryDatabase("FullAppInvoicesDb")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<IInvoiceCommands, InvoiceCommands>();
        services.AddScoped<IInvoiceQueries, InvoiceQueries>();

        return services;
    }
}
