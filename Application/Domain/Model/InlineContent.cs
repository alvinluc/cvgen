namespace Application.Domain.Model
{
    public class InlineContent
    {
        public string Text { get; set; } = string.Empty;
        public string? Url { get; set; }

        public bool IsLink => Url != null;
    }
}
