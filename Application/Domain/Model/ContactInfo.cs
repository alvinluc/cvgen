using System.Collections.Generic;

namespace Application.Domain.Model
{
    public class ContactInfo
    {
        public List<List<InlineContent>> LeftColumn { get; set; } = new();
        public List<List<InlineContent>> RightColumn { get; set; } = new();
    }
}
