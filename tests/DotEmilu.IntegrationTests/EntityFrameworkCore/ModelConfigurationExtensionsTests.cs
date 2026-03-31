using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using DotEmilu.EntityFrameworkCore;
using DotEmilu.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DotEmilu.IntegrationTests.EntityFrameworkCore;

public sealed class ModelBuilderExtensionsTests
{
    private static readonly Type ExternalAssemblyEntityType =
        DynamicEntityAssemblyFactory.CreateBaseEntityType("ExternalAssemblyEntity");

    [Fact]
    public void ApplyBaseEntityConfiguration_Generic_WhenCalled_ThenConfiguresKeySoftDeleteAndVersion()
    {
        var options = new DbContextOptionsBuilder<GenericBaseEntityContext>()
            .UseInMemoryDatabase($"BaseEntity_{Guid.NewGuid()}")
            .Options;

        using var db = new GenericBaseEntityContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(GenericBaseEntity))!;

        Assert.NotNull(entityType.FindPrimaryKey());

        var id = entityType.FindProperty(nameof(GenericBaseEntity.Id))!;
        Assert.Equal(0, id.GetColumnOrder());

        var isDeleted = entityType.FindProperty(nameof(GenericBaseEntity.IsDeleted))!;
        Assert.Equal(1, isDeleted.GetColumnOrder());
        Assert.True(QueryFilterTestHelpers.HasQueryFilter(entityType));

        var version = entityType.FindProperty("Version")!;
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
    }

    [Fact]
    public void ApplyBaseAuditableEntityConfiguration_Generic_WhenCalled_ThenConfiguresKeylessAndAuditFields()
    {
        var options = new DbContextOptionsBuilder<GenericAuditableContext>()
            .UseInMemoryDatabase($"AuditableConfig_{Guid.NewGuid()}")
            .Options;

        using var db = new GenericAuditableContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(GenericAuditableEntity))!;

        Assert.Null(entityType.FindPrimaryKey());
        Assert.True(QueryFilterTestHelpers.HasQueryFilter(entityType));

        Assert.False(entityType.FindProperty(nameof(GenericAuditableEntity.Created))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(GenericAuditableEntity.CreatedBy))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(GenericAuditableEntity.LastModified))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(GenericAuditableEntity.LastModifiedBy))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(GenericAuditableEntity.Deleted))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(GenericAuditableEntity.DeletedBy))!.IsNullable);
    }

    [Fact]
    public void
        ApplyBaseAuditableEntityConfiguration_ThenApplyBaseEntityConfiguration_WhenOrdered_ThenRestoresPrimaryKey()
    {
        var options = new DbContextOptionsBuilder<OrderedGenericPipelineContext>()
            .UseInMemoryDatabase($"OrderedGenericPipeline_{Guid.NewGuid()}")
            .Options;

        using var db = new OrderedGenericPipelineContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(OrderedPipelineEntity))!;
        var primaryKey = entityType.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(OrderedPipelineEntity.Id), primaryKey!.Properties.Single().Name);
    }

    [Fact]
    public void ApplyBaseEntityConfiguration_AssemblyOverload_WhenUsingDynamicAssembly_ThenConfiguresProvidedEntities()
    {
        var options = new DbContextOptionsBuilder<AssemblyOverloadContext>()
            .UseInMemoryDatabase($"AssemblyOverload_{Guid.NewGuid()}")
            .Options;

        using var db = new AssemblyOverloadContext(options);
        var designModel = db.GetService<IDesignTimeModel>().Model;
        var externalEntityType = designModel.FindEntityType(ExternalAssemblyEntityType);

        Assert.NotNull(externalEntityType);
        Assert.NotNull(externalEntityType!.FindPrimaryKey());
    }

    private sealed class GenericBaseEntityContext(DbContextOptions<GenericBaseEntityContext> options)
        : DbContext(options)
    {
        public DbSet<GenericBaseEntity> Entities => Set<GenericBaseEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GenericBaseEntity>();
            modelBuilder.ApplyBaseEntityConfiguration<Guid>(MappingStrategy.Tph, enableRowVersion: true);
        }
    }

    private sealed class GenericAuditableContext(DbContextOptions<GenericAuditableContext> options) : DbContext(options)
    {
        public DbSet<GenericAuditableEntity> Entities => Set<GenericAuditableEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GenericAuditableEntity>();
            modelBuilder.ApplyBaseAuditableEntityConfiguration<Guid>(MappingStrategy.Tph);
        }
    }

    public sealed class GenericBaseEntity : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class GenericAuditableEntity : BaseAuditableEntity<Guid, Guid>
    {
        public string Title { get; set; } = string.Empty;
    }

    public sealed class OrderedPipelineEntity : BaseAuditableEntity<int, Guid>
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class OrderedGenericPipelineContext(DbContextOptions<OrderedGenericPipelineContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderedPipelineEntity>();
            modelBuilder
                .ApplyBaseAuditableEntityConfiguration<Guid>(MappingStrategy.Tph)
                .ApplyBaseEntityConfiguration<int>(MappingStrategy.Tph, enableRowVersion: false);
        }
    }

    private sealed class AssemblyOverloadContext(DbContextOptions<AssemblyOverloadContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .ApplyBaseEntityConfiguration(ExternalAssemblyEntityType.Assembly,
                    new Dictionary<Type, (MappingStrategy strategy, bool enableRowVersion)>
                    {
                        { typeof(Guid), (MappingStrategy.Tph, false) }
                    });
        }
    }
}

public sealed class EntityTypeBuilderExtensionsTests
{
    [Fact]
    public void UseIsDeleted_WithShortConversion_WhenApplied_ThenConfiguresConverterDefaultFilterAndIndex()
    {
        var options = new DbContextOptionsBuilder<IsDeletedContext>()
            .UseInMemoryDatabase($"IsDeleted_{Guid.NewGuid()}")
            .Options;

        using var db = new IsDeletedContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(IsDeletedEntity))!;

        var isDeleted = entityType.FindProperty(nameof(IsDeletedEntity.IsDeleted))!;

        Assert.NotNull(isDeleted.GetValueConverter());
        Assert.Equal(typeof(short), isDeleted.GetValueConverter()!.ProviderClrType);

        var defaultValue = isDeleted.GetDefaultValue();
        Assert.True(
            Equals(defaultValue, false) ||
            Equals(defaultValue, (short)0) ||
            Equals(defaultValue, 0));

        Assert.Equal(2, isDeleted.GetColumnOrder());
        Assert.True(QueryFilterTestHelpers.HasQueryFilter(entityType));
        Assert.Contains(entityType.GetIndexes(),
            i => i.Properties.Any(p => p.Name == nameof(IsDeletedEntity.IsDeleted)));
    }

    [Fact]
    public void UseIsDeleted_WhenUseQueryFilterIsFalse_ThenDoesNotConfigureQueryFilter()
    {
        var options = new DbContextOptionsBuilder<NoFilterIsDeletedContext>()
            .UseInMemoryDatabase($"NoFilter_{Guid.NewGuid()}")
            .Options;

        using var db = new NoFilterIsDeletedContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(NoFilterEntity))!;

        Assert.False(QueryFilterTestHelpers.HasQueryFilter(entityType));
    }

    [Fact]
    public void UseIsDeleted_WhenUseIndexIsFalse_ThenDoesNotConfigureIndex()
    {
        var options = new DbContextOptionsBuilder<NoIndexIsDeletedContext>()
            .UseInMemoryDatabase($"NoIndex_{Guid.NewGuid()}")
            .Options;

        using var db = new NoIndexIsDeletedContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(NoIndexEntity))!;

        Assert.DoesNotContain(entityType.GetIndexes(),
            i => i.Properties.Any(p => p.Name == nameof(NoIndexEntity.IsDeleted)));
    }

    [Fact]
    public void ApplyMappingStrategy_WhenTph_ThenStoresTphAnnotation()
    {
        var annotation = ReadMappingStrategy<TphMappingContext, MappingStrategyEntity>();
        Assert.Equal("TPH", annotation);
    }

    [Fact]
    public void ApplyMappingStrategy_WhenTpt_ThenStoresTptAnnotation()
    {
        var annotation = ReadMappingStrategy<TptMappingContext, MappingStrategyEntity>();
        Assert.Equal("TPT", annotation);
    }

    [Fact]
    public void ApplyMappingStrategy_WhenTpc_ThenStoresTpcAnnotation()
    {
        var annotation = ReadMappingStrategy<TpcMappingContext, MappingStrategyEntity>();
        Assert.Equal("TPC", annotation);
    }

    private static string? ReadMappingStrategy<TContext, TEntity>() where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase($"Mapping_{typeof(TContext).Name}_{Guid.NewGuid()}")
            .Options;

        using var db = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(TEntity))!;
        return entityType.FindAnnotation("Relational:MappingStrategy")?.Value?.ToString();
    }

    private sealed class IsDeletedContext(DbContextOptions<IsDeletedContext> options) : DbContext(options)
    {
        public DbSet<IsDeletedEntity> Entities => Set<IsDeletedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<IsDeletedEntity>(b => b.UseIsDeleted(useShort: true, order: 2));
    }

    private sealed class NoFilterIsDeletedContext(DbContextOptions<NoFilterIsDeletedContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<NoFilterEntity>(b => b.UseIsDeleted(useQueryFilter: false));
    }

    private sealed class NoIndexIsDeletedContext(DbContextOptions<NoIndexIsDeletedContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<NoIndexEntity>(b => b.UseIsDeleted(useIndex: false));
    }

    private sealed class TphMappingContext(DbContextOptions<TphMappingContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<MappingStrategyEntity>(b => b.ApplyMappingStrategy(MappingStrategy.Tph));
    }

    private sealed class TptMappingContext(DbContextOptions<TptMappingContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<MappingStrategyEntity>(b => b.ApplyMappingStrategy(MappingStrategy.Tpt));
    }

    private sealed class TpcMappingContext(DbContextOptions<TpcMappingContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<MappingStrategyEntity>(b => b.ApplyMappingStrategy(MappingStrategy.Tpc));
    }

    private sealed class IsDeletedEntity : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NoFilterEntity : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NoIndexEntity : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class MappingStrategyEntity : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }
}

public sealed class PropertyBuilderExtensionsTests
{
    [Fact]
    public void HasShortConversion_WhenApplied_ThenConvertsTrueToOneAndBack()
    {
        var options = new DbContextOptionsBuilder<ShortConversionContext>()
            .UseInMemoryDatabase($"ShortConv_{Guid.NewGuid()}")
            .Options;

        using var db = new ShortConversionContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ShortConversionEntity))!;
        var isActive = entityType.FindProperty(nameof(ShortConversionEntity.IsActive))!;
        var converter = isActive.GetValueConverter()!;

        var toProvider = converter.ConvertToProvider;
        var fromProvider = converter.ConvertFromProvider;

        Assert.Equal((short)1, (short)toProvider(true)!);
        Assert.Equal((short)0, (short)toProvider(false)!);
        Assert.True((bool)fromProvider((short)1)!);
        Assert.False((bool)fromProvider((short)0)!);
    }

    [Fact]
    public void HasFormattedComment_WhenIncludeTitleAndDescriptions_ThenBuildsExpectedComment()
    {
        var options = new DbContextOptionsBuilder<EnumCommentContext>()
            .UseInMemoryDatabase($"EnumComment_{Guid.NewGuid()}")
            .Options;

        using var db = new EnumCommentContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(EnumCommentEntity))!;
        var status = entityType.FindProperty(nameof(EnumCommentEntity.Status))!;

        var comment = status.GetComment() ?? string.Empty;
        Assert.Contains("Status values", comment);
        Assert.Contains("0 = Zero", comment);
        Assert.Contains("1 = One", comment);
    }

    [Fact]
    public void HasFormattedComment_WhenNullableEnumProperty_ThenBuildsExpectedComment()
    {
        var options = new DbContextOptionsBuilder<NullableEnumCommentContext>()
            .UseInMemoryDatabase($"NullableEnumComment_{Guid.NewGuid()}")
            .Options;

        using var db = new NullableEnumCommentContext(options);
        var entityType = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(NullableEnumCommentEntity))!;
        var status = entityType.FindProperty(nameof(NullableEnumCommentEntity.OptionalStatus))!;

        var comment = status.GetComment() ?? string.Empty;
        Assert.Contains("Status values", comment);
        Assert.Contains("0 = Zero", comment);
        Assert.Contains("1 = One", comment);
    }

    private sealed class ShortConversionContext(DbContextOptions<ShortConversionContext> options) : DbContext(options)
    {
        public DbSet<ShortConversionEntity> Entities => Set<ShortConversionEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<ShortConversionEntity>(b => b.Property(x => x.IsActive).HasShortConversion());
    }

    private sealed class EnumCommentContext(DbContextOptions<EnumCommentContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<EnumCommentEntity>(b =>
                b.Property(x => x.Status).HasFormattedComment("{0} = {2}", includeTitle: true));
    }

    private sealed class NullableEnumCommentContext(DbContextOptions<NullableEnumCommentContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<NullableEnumCommentEntity>(b =>
                b.Property(x => x.OptionalStatus).HasFormattedComment("{0} = {2}", includeTitle: true));
    }

    private sealed class ShortConversionEntity : BaseEntity<Guid>
    {
        public bool IsActive { get; set; }
    }

    private sealed class EnumCommentEntity : BaseEntity<Guid>
    {
        public TestStatus Status { get; set; }
    }

    private sealed class NullableEnumCommentEntity : BaseEntity<Guid>
    {
        public TestStatus? OptionalStatus { get; set; }
    }

    [Description("Status values")]
    private enum TestStatus
    {
        [Description("Zero")] Zero = 0,

        [Description("One")] One = 1
    }
}

internal static class DynamicEntityAssemblyFactory
{
    internal static Type CreateBaseEntityType(string typeName)
    {
        var dynamicAssemblyName = new AssemblyName($"{typeName}_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(dynamicAssemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(dynamicAssemblyName.Name!);

        var interfaceType = typeof(IBaseEntity<Guid>);
        var typeBuilder = moduleBuilder.DefineType(typeName,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        typeBuilder.AddInterfaceImplementation(interfaceType);

        DefineAutoProperty(typeBuilder,
            nameof(IBaseEntity<Guid>.Id),
            typeof(Guid),
            interfaceType.GetProperty(nameof(IBaseEntity<Guid>.Id))!);

        DefineAutoProperty(typeBuilder,
            nameof(IBaseEntity.IsDeleted),
            typeof(bool),
            typeof(IBaseEntity).GetProperty(nameof(IBaseEntity.IsDeleted))!);

        return typeBuilder.CreateType()!;
    }

    private static void DefineAutoProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType,
        PropertyInfo interfaceProperty)
    {
        var backingField = typeBuilder.DefineField($"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}",
            propertyType, FieldAttributes.Private);

        var property = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);

        var getter = typeBuilder.DefineMethod(
            $"get_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
            MethodAttributes.Virtual,
            propertyType,
            Type.EmptyTypes);

        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldarg_0);
        getterIl.Emit(OpCodes.Ldfld, backingField);
        getterIl.Emit(OpCodes.Ret);

        var setter = typeBuilder.DefineMethod(
            $"set_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
            MethodAttributes.Virtual,
            null,
            [propertyType]);

        var setterIl = setter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Ldarg_1);
        setterIl.Emit(OpCodes.Stfld, backingField);
        setterIl.Emit(OpCodes.Ret);

        property.SetGetMethod(getter);
        property.SetSetMethod(setter);

        if (interfaceProperty.GetMethod is not null)
            typeBuilder.DefineMethodOverride(getter, interfaceProperty.GetMethod);

        if (interfaceProperty.SetMethod is not null)
            typeBuilder.DefineMethodOverride(setter, interfaceProperty.SetMethod);
    }
}

internal static class QueryFilterTestHelpers
{
    internal static bool HasQueryFilter(IReadOnlyEntityType entityType)
    {
        var declaredQueryFiltersMethod = entityType.GetType().GetMethod(
            "GetDeclaredQueryFilters",
            BindingFlags.Public | BindingFlags.Instance);

        if (declaredQueryFiltersMethod is not null)
        {
            var declared = declaredQueryFiltersMethod.Invoke(entityType, null) as System.Collections.IEnumerable;
            return declared is not null && declared.Cast<object>().Any();
        }

        var getQueryFilterMethod = entityType.GetType().GetMethod(
            "GetQueryFilter",
            BindingFlags.Public | BindingFlags.Instance);

        return getQueryFilterMethod?.Invoke(entityType, null) is not null;
    }
}
