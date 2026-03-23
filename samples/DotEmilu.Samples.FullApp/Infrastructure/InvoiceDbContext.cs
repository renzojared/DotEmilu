using System.Reflection;
using DotEmilu.EntityFrameworkCore;
using DotEmilu.EntityFrameworkCore.Extensions;
using DotEmilu.Samples.Domain;
using DotEmilu.Samples.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Infrastructure;

/// <summary>
/// EF Core DbContext for the FullApp sample, implementing IUnitOfWork.
/// </summary>
public class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var domainAssembly = DomainAssembly.Instance;
        var currentAssembly = Assembly.GetExecutingAssembly();

        modelBuilder
            // 1) Auditable first: applies audit fields and temporary keyless behavior.
            .ApplyBaseAuditableEntityConfiguration(domainAssembly,
                new Dictionary<Type, MappingStrategy> { { typeof(Guid), MappingStrategy.Tph } })
            // 2) Base entity second: restores key + applies final mapping strategy and soft-delete base config.
            .ApplyBaseEntityConfiguration(domainAssembly,
                new Dictionary<Type, (MappingStrategy, bool)> { { typeof(int), (MappingStrategy.Tph, false) } })
            // 3) Business config last so custom rules can override defaults.
            .ApplyConfigurationsFromAssembly(currentAssembly);
    }
}
