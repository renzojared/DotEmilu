namespace DotEmilu.AspNetCore;

/// <summary>
/// Configuration options for standardizing HTTP result messages, such as Problem Details.
/// </summary>
public class ResultMessage
{
    /// <summary>The configuration section key for these options.</summary>
    public const string SectionKey = nameof(ResultMessage);

    /// <summary>Gets or sets the message details for validation errors.</summary>
    [Required]
    public required Message ValidationError { get; set; }

    /// <summary>Gets or sets the message details for server errors.</summary>
    [Required]
    public required Message ServerError { get; set; }

    /// <summary>
    /// Represents the structure of a specific error message type.
    /// </summary>
    public class Message
    {
        /// <summary>Gets or sets the title of the error message.</summary>
        [Required]
        public required string Title { get; set; }

        /// <summary>Gets or sets the detailed description of the error.</summary>
        [Required]
        public required string Detail { get; set; }

        /// <summary>Gets or sets the URI reference that identifies the error type.</summary>
        public string? Type { get; set; }
    }
}