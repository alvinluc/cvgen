namespace Application.Domain
{
    public class DocumentFactory : IDocumentFactory
    {
        public IFormatter Create(Options opts)
        {
            if (opts.Docx) return new DocXFormatter();
            if (opts.Txt) return new TextFormatter();
            return new PdfFormatter();
        }
    }
}