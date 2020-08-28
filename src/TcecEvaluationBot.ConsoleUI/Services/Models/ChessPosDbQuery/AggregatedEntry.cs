using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public class AggregatedEntry
    {
        public ulong Count { get; set; }
        public ulong WinCount { get; set; }
        public ulong DrawCount { get; set; }
        public ulong LossCount { get; set; }
        public long TotalEloDiff { get; set; }
        public Optional<GameHeader> FirstGame { get; set; }
        public double Perf { get { return (WinCount + DrawCount / 2.0) / Count; } }
        public double DrawRate { get { return (double)DrawCount / Count; } }

        public AggregatedEntry()
        {
            Count = 0;
            WinCount = 0;
            DrawCount = 0;
            LossCount = 0;
            TotalEloDiff = 0;
            FirstGame = Optional<GameHeader>.CreateEmpty();
        }

        public AggregatedEntry(SegregatedEntries entries, List<GameLevel> levels) :
            this()
        {
            foreach ((Origin origin, Entry entry) in entries)
            {
                if (levels.Contains(origin.Level))
                {
                    Combine(entry, origin.Result);
                }
            }
        }

        public AggregatedEntry(SegregatedEntries entries, GameLevel level) :
            this()
        {
            foreach ((Origin origin, Entry entry) in entries)
            {
                if (origin.Level == level)
                {
                    Combine(entry, origin.Result);
                }
            }
        }

        public static AggregatedEntry operator-(AggregatedEntry lhs, AggregatedEntry rhs)
        {
            return new AggregatedEntry
            {
                Count = lhs.Count - rhs.Count,
                WinCount = lhs.WinCount - rhs.WinCount,
                DrawCount = lhs.DrawCount - rhs.DrawCount,
                LossCount = lhs.LossCount - rhs.LossCount,
                TotalEloDiff = lhs.TotalEloDiff - rhs.TotalEloDiff
            };
        }

        public void Combine(Entry entry, GameResult result)
        {
            Count += entry.Count;
            TotalEloDiff += entry.EloDiff.Or(0);

            switch (result)
            {
                case GameResult.WhiteWin:
                    WinCount += entry.Count;
                    break;
                case GameResult.Draw:
                    DrawCount += entry.Count;
                    break;
                case GameResult.BlackWin:
                    LossCount += entry.Count;
                    break;
            }

            if (FirstGame.Count() == 0)
            {
                FirstGame = entry.FirstGame;
            }
            else if (entry.FirstGame.Count() != 0 && entry.FirstGame.First().GameId < FirstGame.First().GameId)
            {
                FirstGame = entry.FirstGame;
            }
        }

        public void Combine(AggregatedEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            Count += entry.Count;
            WinCount += entry.WinCount;
            DrawCount += entry.DrawCount;
            LossCount += entry.LossCount;
            TotalEloDiff += entry.TotalEloDiff;

            if (FirstGame.Count() == 0)
            {
                FirstGame = entry.FirstGame;
            }
            else if (entry.FirstGame.Count() != 0 && entry.FirstGame.First().IsBefore(FirstGame.First()))
            {
                FirstGame = entry.FirstGame;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"+{WinCount}={DrawCount}-{LossCount} ");
            foreach (var game in FirstGame)
            {
                sb.Append(game.ToString());
            }

            return sb.ToString();
        }
    }
}
