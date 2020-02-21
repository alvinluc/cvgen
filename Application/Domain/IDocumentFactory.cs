namespace Application.Domain
{
    public interface IDocumentFactory
    {
        IFormatter Create(Options opts);
    }
}