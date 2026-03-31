using DotEmilu.EntityFrameworkCore;
using DotEmilu.Samples.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotEmilu.Samples.FullApp.Infrastructure.Persistence;

public interface IInvoiceEntities
{
    DbSet<Invoice> Invoices { get; }
}

public interface IInvoiceCommands : IInvoiceEntities, IUnitOfWork;

public interface IInvoiceQueries : IInvoiceEntities;
