using Application.Domain.Model;

namespace Application.Domain.Rendering
{
    public interface IDocumentRenderer
    {
        void Render(CvDocument document, string outputPath);
    }
}
