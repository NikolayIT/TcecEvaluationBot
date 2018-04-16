namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models;
    using TcecEvaluationBot.ConsoleUI.Settings;

    public class GamesCommand : BaseCommand
    {
        private readonly GamesInfoProvider gamesInfoProvider;

        public GamesCommand(Settings settings)
        {
            this.gamesInfoProvider = new GamesInfoProvider(settings.ScheduleUrl);
        }

        public override string Execute(string message)
        {
            GamesList games;
            try
            {
                games = this.gamesInfoProvider.GetGames();
            }
            catch (Exception)
            {
                return "Unable to retrieve schedule data.";
            }

            if (games.CountPlayed == 0)
            {
                return "No games played.";
            }

            var messageParts = message.Split(" ");
            return messageParts.Length == 1 ? this.GetAllStats() : this.GetEngineStats(messageParts.Skip(1));
        }

        private string GetEngineStats(IEnumerable<string> engineNames)
        {
            var engineName = (string)null;
            var games = this.gamesInfoProvider.GetGames();
            foreach (var name in engineNames)
            {
                if (games.Games.Any(x => x.WhiteName.ToLower().StartsWith(name.ToLower().Trim())))
                {
                    engineName = games.Games
                        .FirstOrDefault(x => x.WhiteName.ToLower().StartsWith(name.ToLower().Trim()))?.WhiteName;
                    break;
                }
            }

            if (engineName == null)
            {
                return "Engine with that name was not found!";
            }

            var draws = games.Games.Count(x => x.Result == "1/2 1/2" && (x.WhiteName == engineName || x.BlackName == engineName));
            var wins = games.Games.Count(x => x.WhiteName == engineName && x.Result == "1 0")
                       + games.Games.Count(x => x.BlackName == engineName && x.Result == "0 1");
            var losses = games.Games.Count(x => x.WhiteName == engineName && x.Result == "0 1")
                       + games.Games.Count(x => x.BlackName == engineName && x.Result == "1 0");
            var points = wins + (0.5M * draws);

            var gamesCount = wins + draws + losses;
            var gamesEndingS = gamesCount != 1 ? 's' : '\0';
            var pointsEndingS = points != 1 ? 's' : '\0';
            return $"\"{engineName}\": {wins + draws + losses} game{gamesEndingS}: +{wins}={draws}-{losses} ({points} point{pointsEndingS})";
        }

        private string GetAllStats()
        {
            var games = this.gamesInfoProvider.GetGames();
            var draws = games.Games.Count(x => x.Result == "1/2 1/2");
            var whiteWins = games.Games.Count(x => x.Result == "1 0");
            var blackWins = games.Games.Count(x => x.Result == "0 1");
            decimal playedGames = draws + whiteWins + blackWins;
            var whitePercentage = playedGames == 0 ? 0.0M : whiteWins / playedGames * 100.0M;
            var blackPercentage = playedGames == 0 ? 0.0M : blackWins / playedGames * 100.0M;
            var drawPercentage = playedGames == 0 ? 0.0M : 100.0M - whitePercentage - blackPercentage;
            return $"{playedGames} played game(s): W:{whiteWins}({whitePercentage:0.0}%) / D:{draws}({drawPercentage:0.0}%) / B:{blackWins}({blackPercentage:0.0}%)";
        }
    }
}
