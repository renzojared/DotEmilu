namespace DotEmilu.Samples.ConsoleApp.Scenarios.S08ParameterlessHandler;

/// <summary>
/// A parameterless handler that simulates seeding reference data into a store.
/// <para>
/// Implements <see cref="IHandler"/> — the zero-argument variant of the handler
/// contract — rather than <see cref="Handler{TRequest}"/>.  This is the right
/// choice when an operation:
/// <list type="bullet">
///   <item>Has no meaningful input (e.g. "seed the catalogue", "warm the cache").</item>
///   <item>Does not require FluentValidation because there is nothing to validate.</item>
///   <item>Does not return a typed response.</item>
/// </list>
/// </para>
/// <remarks>
/// <para>
/// <c>AddHandlers(assembly)</c> only scans for generic <c>IHandler&lt;T&gt;</c> and
/// <c>IHandler&lt;T, TResponse&gt;</c> implementations.  Because <see cref="IHandler"/>
/// is a non-generic interface, this class must be registered <b>explicitly</b> in
/// <see cref="Container"/>:
/// <code>services.TryAddScoped&lt;SeedDataHandler&gt;();</code>
/// The concrete type is used as the service key — there is no interface ambiguity
/// because parameterless handlers are resolved by their concrete class.
/// </para>
/// </remarks>
/// </summary>
internal sealed class SeedDataHandler : IHandler
{
    private static readonly (string Id, string Name, decimal Price)[] Catalogue =
    [
        ("SKU-001", "Wireless Keyboard", 49.99m),
        ("SKU-002", "Ergonomic Mouse", 29.99m),
        ("SKU-003", "USB-C Hub", 39.99m),
        ("SKU-004", "Monitor Stand", 59.99m),
        ("SKU-005", "Webcam HD", 89.99m),
    ];

    /// <inheritdoc />
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("  🌱 [SeedDataHandler] Seeding product catalogue…");

        foreach (var (id, name, price) in Catalogue)
        {
            // Simulate an async I/O write (e.g. DbContext.AddAsync + SaveChangesAsync).
            await Task.Delay(1, cancellationToken);
            Console.WriteLine($"     ✓ Seeded: [{id}] {name} — {price:C}");
        }

        Console.WriteLine($"  ✅ [SeedDataHandler] {Catalogue.Length} products seeded successfully.");
    }
}
