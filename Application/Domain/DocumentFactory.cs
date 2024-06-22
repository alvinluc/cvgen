namespace Application.Domain
{
    public class DocumentFactory : IDocumentFactory
    {
        public IFormatter Create(Options opts)
        {
            if (opts.FileFormat.ToLower() == "doc" || opts.FileFormat.ToLower() == "docx") 
                return new DocXFormatter();

            if (opts.FileFormat.ToLower() == "txt" || opts.FileFormat.ToLower() == "text") 
                return new TextFormatter();
                
            return new PdfFormatter();
        }
    }
}