namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class AggregatedEntry
    {
        public AggregatedEntry()
        {
            this.Count = 0;
            this.WinCount = 0;
            this.DrawCount = 0;
            this.LossCount = 0;
            this.TotalEloDiff = 0;
            this.FirstGame = Optional<GameHeader>.CreateEmpty();
        }

        public AggregatedEntry(SegregatedEntries entries, List<GameLevel> levels)
            : this()
        {
            foreach ((Origin origin, Entry entry) in entries)
            {
                if (levels.Contains(origin.Level))
                {
                    this.Combine(entry, origin.Result);
                }
            }
        }

        public AggregatedEntry(SegregatedEntries entries, GameLevel level)
            : this()
        {
            foreach ((Origin origin, Entry entry) in entries)
            {
                if (origin.Level == level)
                {
                    this.Combine(entry, origin.Result);
                }
            }
        }

        public ulong Count { get; set; }

        public ulong WinCount { get; set; }

        public ulong DrawCount { get; set; }

        public ulong LossCount { get; set; }

        public long TotalEloDiff { get; set; }

        public Optional<GameHeader> FirstGame { get; set; }

        public double Perf
        {
            get { return (this.WinCount + (this.DrawCount / 2.0)) / this.Count; }
        }

        public double DrawRate
        {
            get { return (double)this.DrawCount / this.Count; }
        }

        public static AggregatedEntry operator -(AggregatedEntry lhs, AggregatedEntry rhs)
        {
            return new AggregatedEntry
            {
                Count = lhs.Count - rhs.Count,
                WinCount = lhs.WinCount - rhs.WinCount,
                DrawCount = lhs.DrawCount - rhs.DrawCount,
                LossCount = lhs.LossCount - rhs.LossCount,
                TotalEloDiff = lhs.TotalEloDiff - rhs.TotalEloDiff,
            };
        }

        public void Combine(Entry entry, GameResult result)
        {
            this.Count += entry.Count;
            this.TotalEloDiff += entry.EloDiff.Or(0);

            switch (result)
            {
                case GameResult.WhiteWin:
                    this.WinCount += entry.Count;
                    break;
                case GameResult.Draw:
                    this.DrawCount += entry.Count;
                    break;
                case GameResult.BlackWin:
                    this.LossCount += entry.Count;
                    break;
            }

            if (this.FirstGame.Count() == 0)
            {
                this.FirstGame = entry.FirstGame;
            }
            else if (entry.FirstGame.Count() != 0 && entry.FirstGame.First().GameId < this.FirstGame.First().GameId)
            {
                this.FirstGame = entry.FirstGame;
            }
        }

        public void Combine(AggregatedEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            this.Count += entry.Count;
            this.WinCount += entry.WinCount;
            this.DrawCount += entry.DrawCount;
            this.LossCount += entry.LossCount;
            this.TotalEloDiff += entry.TotalEloDiff;

            if (this.FirstGame.Count() == 0)
            {
                this.FirstGame = entry.FirstGame;
            }
            else if (entry.FirstGame.Count() != 0 && entry.FirstGame.First().IsBefore(this.FirstGame.First()))
            {
                this.FirstGame = entry.FirstGame;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"+{this.WinCount}={this.DrawCount}-{this.LossCount} ");
            foreach (var game in this.FirstGame)
            {
                sb.Append(game.ToString());
            }

            return sb.ToString();
        }
    }
}
