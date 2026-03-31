using DotEmilu.EntityFrameworkCore;
using DotEmilu.EntityFrameworkCore.Extensions;

namespace DotEmilu.IntegrationTests.Fixtures;

/// <summary>
/// In-memory DbContext for integration testing EF Core interceptors and query extensions.
/// </summary>
public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<TestAuditableEntity> TestAuditableEntities => Set<TestAuditableEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>(builder =>
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
            builder.UseIsDeleted(useQueryFilter: true);
        });

        modelBuilder.Entity<TestAuditableEntity>(builder =>
        {
            new BaseAuditableEntityConfiguration<TestAuditableEntity, Guid>(MappingStrategy.Tph)
                .Configure(builder);

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.Title).HasMaxLength(100).IsRequired();
        });
    }
}
