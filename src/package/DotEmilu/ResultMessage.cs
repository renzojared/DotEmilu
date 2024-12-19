namespace DotEmilu;

public class ResultMessage
{
    public const string SectionKey = nameof(ResultMessage);
    public required Message ValidationError { get; set; }
    public required Message ServerError { get; set; }

    public class Message
    {
        public required string Title { get; set; }
        public required string Detail { get; set; }
        public string? Type { get; set; }
    }
}