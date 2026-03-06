using System.Collections.Generic;

namespace Application.Domain.Model
{
    public class CvSection
    {
        public string Title { get; set; } = string.Empty;
        public List<ContentBlock> Content { get; set; } = new();
        public List<CvSubSection> SubSections { get; set; } = new();
    }
}
