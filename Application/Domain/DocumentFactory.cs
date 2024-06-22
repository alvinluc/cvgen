namespace Application.Domain
{
    public class DocumentFactory : IDocumentFactory
    {
        public IFormatter Create(string? fileFormat)
        {
            if (fileFormat?.ToLower() == "doc" || fileFormat?.ToLower() == "docx") 
                return new DocXFormatter();

            if (fileFormat?.ToLower() == "txt" || fileFormat?.ToLower() == "text") 
                return new TextFormatter();
                
            return new PdfFormatter();
        }
    }
}