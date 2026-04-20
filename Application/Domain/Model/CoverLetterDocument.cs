using System.Collections.Generic;

namespace Application.Domain.Model
{
    public class CoverLetterDocument
    {
        public string Name { get; set; } = string.Empty;
        public ContactInfo Contact { get; set; } = new();
        public string Date { get; set; } = string.Empty;
        public List<List<InlineContent>> Recipient { get; set; } = new();
        public List<InlineContent> Subject { get; set; } = new();
        public List<InlineContent> Salutation { get; set; } = new();
        public List<List<InlineContent>> Body { get; set; } = new();
        public List<InlineContent> SignOff { get; set; } = new();
    }
}
