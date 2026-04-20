using Application.Domain.Model;

namespace Application.Domain.Rendering
{
    public interface ICoverLetterRenderer
    {
        void Render(CoverLetterDocument document, string outputPath);
    }
}
