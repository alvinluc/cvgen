using System.Collections.Generic;

namespace Application.Domain.Model
{
    public class CvSubSection
    {
        public List<InlineContent> Title { get; set; } = new();
        public List<ContentBlock> Content { get; set; } = new();
    }
}
