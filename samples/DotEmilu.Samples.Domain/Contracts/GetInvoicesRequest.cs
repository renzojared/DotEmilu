using DotEmilu.Samples.Domain.Entities;

namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>
/// Request to retrieve a paginated list of invoices.
/// </summary>
public record GetInvoicesRequest(
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PaginatedList<Invoice>>;
