namespace Application.Domain
{
    public interface IDocumentFactory
    {
        IFormatter Create(string? fileFormat);
    }
}