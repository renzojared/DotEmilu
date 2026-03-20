using System.Text.Json.Serialization;

namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>Request to soft-delete an invoice by Id.</summary>
public sealed record DeleteInvoiceRequest([property: JsonIgnore] int Id) : IRequest;
