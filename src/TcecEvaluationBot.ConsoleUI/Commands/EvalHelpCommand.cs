namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;

    public class EvalHelpCommand : BaseCommand
    {
        public override string Execute(string message)
        {
            return $@"[{DateTime.UtcNow:HH:mm:ss}] Available commands: 
!eval [engine] [time] - evaluates current position; 
!time [gameNum] - estimates start time for a given game or the end time for the division; 
!games [engine] - calculates wins/draws/loses for a given engine; 
!rand [min] [max] - generates a random number between min and max; 
!db - looks up current position in the Lichess DB; 
!static - runs static evaluation for the current position with SF";
        }
    }
}
