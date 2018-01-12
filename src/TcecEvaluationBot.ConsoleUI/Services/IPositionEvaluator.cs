namespace TcecEvaluationBot.ConsoleUI.Services
{
    public interface IPositionEvaluator
    {
        string GetEvaluation(string fenPosition, int moveTime);
    }
}
