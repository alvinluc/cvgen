using Application.Domain.Rendering;

namespace Application.Domain
{
    public class CoverLetterFactory : ICoverLetterFactory
    {
        public ICoverLetterRenderer Create(string? fileFormat)
        {
            if (fileFormat?.ToLower() == "doc" || fileFormat?.ToLower() == "docx")
                return new CoverLetterDocxRenderer();

            if (fileFormat?.ToLower() == "txt" || fileFormat?.ToLower() == "text")
                return new CoverLetterTextRenderer();

            return new CoverLetterPdfRenderer();
        }
    }
}
