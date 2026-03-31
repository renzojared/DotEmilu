using System.Reflection;
using System.Reflection.Emit;
using DotEmilu.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.UnitTests.DiContainer;

public class EfCoreDiContainerTests
{
    [Fact]
    public void AddSoftDeleteInterceptor_WhenCalled_ThenRegistersInterceptor()
    {
        var services = new ServiceCollection();

        services.AddSoftDeleteInterceptor();

        var provider = services.BuildServiceProvider();
        var interceptors = provider.GetServices<ISaveChangesInterceptor>();
        Assert.Contains(interceptors, i => i is SoftDeleteInterceptor);
    }

    [Fact]
    public void AddAuditableEntityInterceptor_WhenCalled_ThenRegistersInterceptorAndContextUser()
    {
        var services = new ServiceCollection();

        services.AddAuditableEntityInterceptor<LongContextUser, long>();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IContextUser<long>>());
        var interceptors = provider.GetServices<ISaveChangesInterceptor>();
        Assert.Contains(interceptors, i => i is AuditableEntityInterceptor<long>);
    }

    [Fact]
    public void AddAuditableEntityInterceptors_WhenContextUserIsIndirectlyImplemented_ThenRegistersIt()
    {
        var services = new ServiceCollection();

        services.AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        var contextUser = provider.GetService<IContextUser<Guid>>();
        var interceptors = provider.GetServices<ISaveChangesInterceptor>();

        Assert.NotNull(contextUser);
        Assert.IsType<IndirectContextUser>(contextUser);
        Assert.Contains(interceptors, i => i is AuditableEntityInterceptor<Guid>);
    }

    [Fact]
    public void AddAuditableEntityInterceptors_WhenDuplicateUserKeysExist_ThenThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var assemblyWithDuplicates =
            BuildDynamicAssemblyWithContextUsers("DuplicateGuidContextUsers", duplicateGuid: true);

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddAuditableEntityInterceptors(assemblyWithDuplicates));

        Assert.Contains("Guid", exception.Message);
    }

    public interface IAppContextUser : IContextUser<Guid>;

    public sealed class IndirectContextUser : IAppContextUser
    {
        public Guid Id => Guid.Empty;
    }

    public sealed class LongContextUser : IContextUser<long>
    {
        public long Id => 7L;
    }

    private static AssemblyBuilder BuildDynamicAssemblyWithContextUsers(string assemblyName, bool duplicateGuid)
    {
        var dynamicAssemblyName = new AssemblyName($"{assemblyName}_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(dynamicAssemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(dynamicAssemblyName.Name!);

        CreateContextUserType(moduleBuilder, "GuidContextUserA", typeof(Guid));
        if (duplicateGuid)
            CreateContextUserType(moduleBuilder, "GuidContextUserB", typeof(Guid));

        return assemblyBuilder;
    }

    private static void CreateContextUserType(ModuleBuilder moduleBuilder, string typeName, Type keyType)
    {
        var interfaceType = typeof(IContextUser<>).MakeGenericType(keyType);
        var typeBuilder = moduleBuilder.DefineType(typeName,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        typeBuilder.AddInterfaceImplementation(interfaceType);

        var getter = typeBuilder.DefineMethod(
            "get_Id",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
            MethodAttributes.Virtual,
            keyType,
            Type.EmptyTypes);

        var il = getter.GetILGenerator();
        var local = il.DeclareLocal(keyType);
        il.Emit(OpCodes.Ldloca_S, local);
        il.Emit(OpCodes.Initobj, keyType);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ret);

        var property = typeBuilder.DefineProperty("Id", PropertyAttributes.None, keyType, null);
        property.SetGetMethod(getter);

        var interfaceGetter = interfaceType.GetProperty(nameof(IContextUser<Guid>.Id))!.GetMethod!;
        typeBuilder.DefineMethodOverride(getter, interfaceGetter);
        typeBuilder.CreateType();
    }
}
