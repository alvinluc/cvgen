using System.Collections.Generic;

namespace Application.Domain.Model
{
    public class CvSubSection
    {
        public List<InlineContent> Title { get; set; } = new();
        public int Level { get; set; } = 2;
        public List<ContentBlock> Content { get; set; } = new();
    }
}
