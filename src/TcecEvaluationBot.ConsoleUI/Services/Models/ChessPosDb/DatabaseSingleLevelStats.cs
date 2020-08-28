namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;

    using Newtonsoft.Json.Linq;
    using TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery;

    public class DatabaseSingleLevelStats
    {
        public DatabaseSingleLevelStats(JObject json)
        {
            this.NumGames = json["num_games"].Value<ulong>();
            this.NumPositions = json["num_positions"].Value<ulong>();
            this.TotalWhiteElo = json["total_white_elo"].Value<ulong>();
            this.TotalBlackElo = json["total_black_elo"].Value<ulong>();
            this.NumGamesWithElo = json["num_games_with_elo"].Value<ulong>();
            this.NumGamesWithDate = json["num_games_with_date"].Value<ulong>();

            if (this.NumGamesWithElo > 0)
            {
                this.MinElo = json["min_elo"].Value<ulong>();
                this.MaxElo = json["max_elo"].Value<ulong>();
            }

            if (this.NumGamesWithDate > 0)
            {
                this.MinDate = Date.FromString(json["min_date"].Value<string>(), '-');
                this.MaxDate = Date.FromString(json["max_date"].Value<string>(), '-');
            }
        }

        public DatabaseSingleLevelStats(DatabaseSingleLevelStats other)
        {
            this.NumGames = other.NumGames;
            this.NumPositions = other.NumPositions;
            this.TotalWhiteElo = other.TotalWhiteElo;
            this.TotalBlackElo = other.TotalBlackElo;
            this.NumGamesWithElo = other.NumGamesWithElo;
            this.NumGamesWithDate = other.NumGamesWithDate;
            this.MinElo = other.MinElo;
            this.MaxElo = other.MaxElo;
            this.MinDate = other.MinDate;
            this.MaxDate = other.MaxDate;
        }

        public ulong NumGames { get; private set; }

        public ulong NumPositions { get; private set; }

        public ulong TotalWhiteElo { get; private set; }

        public ulong TotalBlackElo { get; private set; }

        public ulong NumGamesWithElo { get; private set; }

        public ulong NumGamesWithDate { get; private set; }

        public ulong MinElo { get; private set; }

        public ulong MaxElo { get; private set; }

        public Date MinDate { get; private set; }

        public Date MaxDate { get; private set; }

        public void Add(DatabaseSingleLevelStats other)
        {
            this.NumGames += other.NumGames;
            this.NumPositions += other.NumPositions;
            this.TotalWhiteElo += other.TotalWhiteElo;
            this.TotalBlackElo += other.TotalBlackElo;

            if (this.NumGamesWithElo == 0)
            {
                this.MinElo = other.MinElo;
                this.MaxElo = other.MaxElo;
            }
            else if (other.NumGamesWithElo != 0)
            {
                this.MinElo = Math.Min(this.MinElo, other.MinElo);
                this.MaxElo = Math.Min(this.MaxElo, other.MaxElo);
            }

            if (this.NumGamesWithDate == 0)
            {
                this.MinDate = other.MinDate;
                this.MaxDate = other.MaxDate;
            }
            else if (other.NumGamesWithDate != 0)
            {
                this.MinDate = Date.Min(this.MinDate, other.MinDate);
                this.MaxDate = Date.Max(this.MaxDate, other.MaxDate);
            }

            this.NumGamesWithElo += other.NumGamesWithElo;
            this.NumGamesWithDate += other.NumGamesWithDate;
        }
    }
}
