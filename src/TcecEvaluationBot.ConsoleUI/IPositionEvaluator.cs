namespace TcecEvaluationBot.ConsoleUI
{
    public interface IPositionEvaluator
    {
        string GetEvaluation(string fenPosition, int moveTime);
    }
}
