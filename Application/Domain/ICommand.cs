namespace Application.Domain
{
    public interface ICommand
    {
        void Execute(string command);
    }
}