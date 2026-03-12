namespace DotEmilu.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extension methods for EF Core's ModelBuilder.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <param name="modelBuilder">The model builder.</param>
    extension(ModelBuilder modelBuilder)
    {
        /// <summary>Applies base entity configurations to all types implementing the IBaseEntity interface.</summary>
        /// <param name="strategy">The inheritance mapping strategy to use.</param>
        /// <param name="enableRowVersion">Whether to enable row versioning.</param>
        /// <typeparam name="TKey">The primary key type.</typeparam>
        /// <returns>The updated model builder.</returns>
        public ModelBuilder ApplyBaseEntityConfiguration<TKey>(MappingStrategy strategy = MappingStrategy.Tpc,
            bool enableRowVersion = false) where TKey : struct
        {
            foreach (var entityType in modelBuilder.Model
                         .GetEntityTypes()
                         .Where(e => typeof(IBaseEntity<TKey>)
                             .IsAssignableFrom(e.ClrType) && !e.ClrType.IsAbstract))
            {
                var configType = typeof(BaseEntityConfiguration<,>)
                    .MakeGenericType(entityType.ClrType, typeof(TKey));
                var configInstance = Activator.CreateInstance(configType, strategy, enableRowVersion);
                modelBuilder.ApplyConfiguration((dynamic)configInstance!);
            }

            return modelBuilder;
        }

        /// <summary>Applies base entity configurations discovered from an assembly.</summary>
        /// <param name="assembly">The assembly to scan for implementers of IBaseEntity.</param>
        /// <param name="keyTypeConfigurations">Custom configurations mapped by primary key type.</param>
        /// <returns>The updated model builder.</returns>
        public ModelBuilder ApplyBaseEntityConfiguration(Assembly assembly,
            Dictionary<Type, (MappingStrategy strategy, bool enableRowVersion)>? keyTypeConfigurations = null)
        {
            var entityTypes = assembly
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                            t.GetInterfaces().Any(IsBaseEntityInterface));

            foreach (var entityType in entityTypes)
            {
                var keyType = entityType.GetInterfaces()
                    .First(IsBaseEntityInterface)
                    .GetGenericArguments()[0];

                var (strategy, enableRowVersion) =
                    keyTypeConfigurations?.TryGetValue(keyType, out var configuration) is true
                        ? configuration
                        : (MappingStrategy.Tpc, false);

                var configType = typeof(BaseEntityConfiguration<,>).MakeGenericType(entityType, keyType);
                var configInstance = Activator.CreateInstance(configType, strategy, enableRowVersion);
                modelBuilder.ApplyConfiguration((dynamic)configInstance!);
            }

            return modelBuilder;

            static bool IsBaseEntityInterface(Type i)
                => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseEntity<>);
        }

        /// <summary>Applies auditable base entity configurations to all types implementing the IBaseAuditableEntity interface.</summary>
        /// <param name="strategy">The inheritance mapping strategy to use.</param>
        /// <typeparam name="TUserKey">The user key type (e.g., Guid or long).</typeparam>
        /// <returns>The updated model builder.</returns>
        public ModelBuilder ApplyBaseAuditableEntityConfiguration<TUserKey>(
            MappingStrategy strategy = MappingStrategy.Tpc) where TUserKey : struct
        {
            foreach (var entityType in modelBuilder.Model
                         .GetEntityTypes()
                         .Where(e => typeof(IBaseAuditableEntity<TUserKey>)
                             .IsAssignableFrom(e.ClrType) && !e.ClrType.IsAbstract))
            {
                var configType = typeof(BaseAuditableEntityConfiguration<,>)
                    .MakeGenericType(entityType.ClrType, typeof(TUserKey));
                var configInstance = Activator.CreateInstance(configType, strategy);
                modelBuilder.ApplyConfiguration((dynamic)configInstance!);
            }

            return modelBuilder;
        }

        /// <summary>Applies auditable base entity configurations discovered from an assembly.</summary>
        /// <param name="assembly">The assembly to scan for implementers of IBaseAuditableEntity.</param>
        /// <param name="userKeyConfigurations">Custom mapping strategies grouped by user key types.</param>
        /// <returns>The updated model builder.</returns>
        public ModelBuilder ApplyBaseAuditableEntityConfiguration(Assembly assembly,
            Dictionary<Type, MappingStrategy>? userKeyConfigurations = null)
        {
            var entityTypes = assembly
                .GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                            t.GetInterfaces().Any(IsBaseAuditableEntityInterface));

            foreach (var entityType in entityTypes)
            {
                var userKeyType = entityType.GetInterfaces()
                    .First(IsBaseAuditableEntityInterface)
                    .GetGenericArguments()[0];

                var strategy = userKeyConfigurations?.TryGetValue(userKeyType, out var config) is true
                    ? config
                    : MappingStrategy.Tpc;

                var configType = typeof(BaseAuditableEntityConfiguration<,>).MakeGenericType(entityType, userKeyType);
                var configInstance = Activator.CreateInstance(configType, strategy);
                modelBuilder.ApplyConfiguration((dynamic)configInstance!);
            }

            return modelBuilder;

            static bool IsBaseAuditableEntityInterface(Type i)
                => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseAuditableEntity<>);
        }
    }
}