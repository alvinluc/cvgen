using System.Collections.Generic;

namespace Application.Domain.Model
{
    public enum ContentBlockType
    {
        Paragraph,
        List,
        DefinitionList
    }

    public class ContentBlock
    {
        public ContentBlockType Type { get; set; }
        public List<List<InlineContent>> Items { get; set; } = new();

        // For definition lists: term -> definition
        public List<(List<InlineContent> Term, List<InlineContent> Definition)> Definitions { get; set; } = new();
    }
}
