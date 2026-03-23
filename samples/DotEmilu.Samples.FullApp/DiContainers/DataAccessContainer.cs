using System.Reflection;
using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.FullApp.Infrastructure;
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
            .AddHttpContextAccessor();

        services.AddDbContext<InvoiceDbContext>((sp, options) =>
            options
                .UseInMemoryDatabase("FullAppInvoicesDb")
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        return services;
    }
}
