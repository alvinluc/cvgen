using Application.Domain.Rendering;

namespace Application.Domain
{
    public interface IDocumentFactory
    {
        IDocumentRenderer Create(string? fileFormat);
    }
}
