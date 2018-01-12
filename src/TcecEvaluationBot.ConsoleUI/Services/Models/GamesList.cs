namespace TcecEvaluationBot.ConsoleUI.Services.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class GamesList
    {
        public GamesList(IList<Game> games)
        {
            this.Games = games;
        }

        public IList<Game> Games { get; set; }

        public int CountPlayed => this.Games.Count(x => x.IsPlayed);

        public int Count => this.Games.Count;

        public TimeSpan TotalTime =>
            this.Games.Where(x => x.Duration.HasValue).Aggregate(
                TimeSpan.Zero,
                (sumSoFar, x) => sumSoFar + x.Duration.Value);

        public TimeSpan AverageGameTime => this.TotalTime / this.CountPlayed;

        public DateTime LastStarted
        {
            get
            {
                var lastStarted = this.Games.Where(x => x.Started.HasValue).Select(x => x.Started.Value)
                    .OrderByDescending(x => x).FirstOrDefault();
                if (this.Games.Count(x => x.Duration.HasValue) == this.Games.Count(x => x.Started.HasValue))
                {
                    return DateTime.UtcNow;
                }

                return lastStarted;
            }
        }
    }
}
