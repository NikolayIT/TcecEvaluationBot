namespace TcecEvaluationBot.ConsoleUI.Commands
{
    public abstract class BaseCommand : ICommand
    {
        public abstract string Execute(string message);
    }
}
