namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess.CqrsPattern;

/// <summary>
/// Read-side interface for query-only access to entities.
/// Demonstrates the CQRS Queries pattern used with DotEmilu.
/// </summary>
public interface IQueries : IEntities;
