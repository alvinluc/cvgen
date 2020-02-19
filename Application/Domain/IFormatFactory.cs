namespace Application.Domain
{
    
    public interface IFormatFactory
    {
        IFormatter Create(string formatType);
    }    
}