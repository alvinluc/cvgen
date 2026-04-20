using Application.Domain.Rendering;

namespace Application.Domain
{
    public interface ICoverLetterFactory
    {
        ICoverLetterRenderer Create(string? fileFormat);
    }
}
