using Application.Domain.Rendering;

namespace Application.Domain
{
    public class DocumentFactory : IDocumentFactory
    {
        public IDocumentRenderer Create(string? fileFormat)
        {
            if (fileFormat?.ToLower() == "doc" || fileFormat?.ToLower() == "docx")
                return new DocxRenderer();

            if (fileFormat?.ToLower() == "txt" || fileFormat?.ToLower() == "text")
                return new TextRenderer();

            return new PdfRenderer();
        }
    }
}
