namespace TcecEvaluationBot.ConsoleUI.Commands
{
    public interface ICommand
    {
        string Execute(string message);
        void Dispose();
    }
}
