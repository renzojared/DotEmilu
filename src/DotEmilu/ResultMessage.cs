namespace DotEmilu;

public class ResultMessage
{
    public const string SectionKey = nameof(ResultMessage);
    [Required]
    public required Message ValidationError { get; set; }
    [Required]
    public required Message ServerError { get; set; }

    public class Message
    {
        [Required]
        public required string Title { get; set; }
        [Required]
        public required string Detail { get; set; }
        public string? Type { get; set; }
    }
}