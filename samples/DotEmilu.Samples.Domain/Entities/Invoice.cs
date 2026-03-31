namespace DotEmilu.Samples.Domain.Entities;

/// <summary>
/// Represents an invoice entity with auditing support.
/// </summary>
public class Invoice : BaseAuditableEntity<int, Guid>
{
    /// <summary>Gets or sets the invoice number.</summary>
    public required string Number { get; set; }

    /// <summary>Gets or sets the invoice description.</summary>
    public required string Description { get; set; }

    /// <summary>Gets or sets the invoice amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the invoice date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets a value indicating whether the invoice has been paid.</summary>
    public bool IsPaid { get; set; }
}