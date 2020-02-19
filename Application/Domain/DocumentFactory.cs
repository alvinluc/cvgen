namespace Application.Domain
{
    public class DocumentFactory : IFormatFactory
    {
        public IFormatter Create(string formatType)
        {
            switch (formatType)
            {
                case "pdf":
                    return new PdfFormatter();
                case "docx":
                    return new DocXFormatter();
                case "txt":
                    return new TextFormatter();
                default:
                    return new PdfFormatter();
            }
        }
    }
}