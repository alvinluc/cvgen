using Application.Domain.Model;

namespace Application.Domain.Parsing
{
    public interface ICoverLetterParser
    {
        CoverLetterDocument Parse(string filePath);
    }
}
