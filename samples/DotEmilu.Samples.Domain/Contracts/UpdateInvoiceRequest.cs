using System.Text.Json.Serialization;

namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>
/// Request to update an existing invoice.
/// The <see cref="Id"/> is excluded from the JSON body and is populated from the route
/// using the <c>request with { Id = id }</c> pattern in the endpoint.
/// </summary>
public record UpdateInvoiceRequest(
    [property: JsonIgnore] int Id,
    string Number,
    string Description,
    decimal Amount,
    DateOnly Date,
    bool IsPaid) : IRequest;
