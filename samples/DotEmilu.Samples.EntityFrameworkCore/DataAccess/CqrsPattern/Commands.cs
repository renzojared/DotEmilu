using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess.CqrsPattern;

/// <summary>
/// Write-side DbContext implementation. Inherits from <see cref="InvoiceDbContext"/>
/// and exposes <see cref="ICommands"/> (entities + unit of work).
/// </summary>
internal sealed class Commands(DbContextOptions<InvoiceDbContext> options) : InvoiceDbContext(options), ICommands;
