using DotEmilu.Samples.Domain.Entities;
using DotEmilu.Samples.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.EntityFrameworkCore.DataAccess.CqrsPattern;

/// <summary>
/// Exposes the entity sets available in the database.
/// Shared by both <see cref="ICommands"/> and <see cref="IQueries"/>.
/// </summary>
public interface IEntities
{
    DbSet<Invoice> Invoices { get; }
    DbSet<Song> Songs { get; }
}
