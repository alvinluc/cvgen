using System.Collections.Generic;

namespace Application.Domain.Model
{
    public class CvDocument
    {
        public string Name { get; set; } = string.Empty;
        public ContactInfo Contact { get; set; } = new();
        public List<CvSection> Sections { get; set; } = new();
    }
}
