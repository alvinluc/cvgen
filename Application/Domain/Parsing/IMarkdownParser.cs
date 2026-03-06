using Application.Domain.Model;

namespace Application.Domain.Parsing
{
    public interface IMarkdownParser
    {
        CvDocument Parse(string filePath);
    }
}
