using DotEmilu.EntityFrameworkCore;

namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess.CqrsPattern;

/// <summary>
/// Write-side interface combining entity access with unit of work.
/// Demonstrates the CQRS Commands pattern used with DotEmilu.
/// </summary>
public interface ICommands : IEntities, IUnitOfWork;
