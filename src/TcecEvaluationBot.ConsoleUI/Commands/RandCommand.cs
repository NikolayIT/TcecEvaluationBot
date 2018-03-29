namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;

    public class RandCommand : ICommand
    {
        private readonly Random random;

        public RandCommand()
        {
            this.random = new Random();
        }

        public string Execute(string message)
        {
            var parts = message.Split(" ");
            if (parts.Length >= 3 && long.TryParse(parts[1], out var firstValue) && long.TryParse(parts[2], out var secondValue))
            {
                var randomNumber = this.LongRandom(Math.Min(firstValue, secondValue), Math.Max(firstValue, secondValue) + 1);
                return
                    $"[{DateTime.UtcNow:HH:mm:ss}] Random number between {Math.Min(firstValue, secondValue)} and {Math.Max(firstValue, secondValue)}: {randomNumber}";
            }
            else
            {
                return
                    $"[{DateTime.UtcNow:HH:mm:ss}] Usage: !eval [minNumber] [maxNumber]";
            }
        }

        private long LongRandom(long min, long max)
        {
            long result = this.random.Next((int)(min >> 32), (int)(max >> 32));
            result = result << 32;
            result = result | (long)this.random.Next((int)min, (int)max);
            return result;
        }
    }
}
