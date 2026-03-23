using System.Reflection;
using DotEmilu.EntityFrameworkCore;
using DotEmilu.EntityFrameworkCore.Extensions;
using DotEmilu.Samples.Domain;
using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.DataAccess.CqrsPattern;
using DotEmilu.Samples.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess;

/// <summary>
/// EF Core DbContext for the Invoice sample, implementing IUnitOfWork for CQRS-style access.
/// </summary>
public class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : DbContext(options), IEntities
{
    /// <summary>Gets the set of invoices.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>Gets the set of songs.</summary>
    public DbSet<Song> Songs => Set<Song>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ════════════════════════════════════════════════════════════════════════════════════════════
        // HOW DOTEMILU'S AUTOMATIC CONFIGURATION SYSTEM WORKS
        // ════════════════════════════════════════════════════════════════════════════════════════════
        // The order of the Apply calls is CRITICAL due to C# interface inheritance.
        // Because IBaseAuditableEntity inherits from IBaseEntity, auditable entities qualify for
        // both configurations. To prevent incorrect overrides (e.g. losing the primary key),
        // the following strict order must be respected:
        //
        // 1. ApplyBaseAuditableEntityConfiguration:
        //    Applies audit-only configuration (Created, CreatedBy, etc.) and temporarily calls
        //    .HasNoKey(), which will be corrected by the base entity configuration below.
        //
        // 2. ApplyBaseEntityConfiguration:
        //    Must run AFTER the auditable config. Restores the primary key (.HasKey), configures
        //    soft-delete (.UseIsDeleted), and OVERRIDES the final mapping strategy (TPH, TPC).
        //
        // 3. ApplyConfigurationsFromAssembly:
        //    Applies business-specific configurations (e.g. InvoiceConfiguration).
        //    Runs last so it can override any base default if needed.
        // ════════════════════════════════════════════════════════════════════════════════════════════

        var domainAssembly = DomainAssembly.Instance;
        var currentAssembly = Assembly.GetExecutingAssembly();

        modelBuilder
            // 1. Auditable Entity Config (scans Domain assembly for IBaseAuditableEntity<> implementors)
            .ApplyBaseAuditableEntityConfiguration(domainAssembly,
                new Dictionary<Type, MappingStrategy> { { typeof(Guid), MappingStrategy.Tph } })

            // 2. Base Entity Config (scans Domain and current assembly for IBaseEntity<> implementors)
            .ApplyBaseEntityConfiguration(domainAssembly,
                new Dictionary<Type, (MappingStrategy, bool)> { { typeof(int), (MappingStrategy.Tph, false) } })
            .ApplyBaseEntityConfiguration(currentAssembly)

            // 3. Business Configs (scans current assembly for IEntityTypeConfiguration<T> implementors)
            .ApplyConfigurationsFromAssembly(currentAssembly);
    }
}
