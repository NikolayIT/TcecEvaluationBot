namespace TcecEvaluationBot.ConsoleUI.Services.Models
{
    using System;

    public class Game
    {
        public string WhiteName { get; set; }

        public string BlackName { get; set; }

        public string Result { get; set; }

        public int Number { get; set; }

        public TimeSpan? Duration { get; set; }

        public DateTime? Started { get; set; }

        public bool IsPlayed => this.Duration.HasValue;
    }
}
