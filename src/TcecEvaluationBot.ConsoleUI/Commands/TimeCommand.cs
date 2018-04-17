namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models;
    using TcecEvaluationBot.ConsoleUI.Settings;

    public class TimeCommand : BaseCommand
    {
        private readonly GamesInfoProvider gamesInfoProvider;

        public TimeCommand(Settings settings)
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
            var gameId = messageParts.Any(x => int.TryParse(x, out _))
                             ? int.Parse(messageParts.FirstOrDefault(x => int.TryParse(x, out _)))
                             : (int?)null;

            if (gameId.HasValue && gameId >= 1 && gameId <= games.Count)
            {
                return GetGameInfo(games, gameId.Value);
            }

            if (messageParts.Contains("last"))
            {
                return GetLastGameInfo(games);
            }

            if (messageParts.Contains("next"))
            {
                return GetNextGameInfo(games);
            }

            return GetRemainingDivisionTime(games);
        }

        private static string GetLastGameInfo(GamesList games)
        {
            var lastGame = games.Games.OrderBy(x => x.Number).LastOrDefault(x => x.IsPlayed);
            return lastGame == null ? "The division has just started." : GetGameInfo(games, lastGame.Number);
        }

        private static string GetNextGameInfo(GamesList games)
        {
            var nextGame = games.Games.OrderBy(x => x.Number).FirstOrDefault(x => !x.Started.HasValue);
            return nextGame == null ? "The next division will start soon." : GetGameInfo(games, nextGame.Number);
        }

        private static string GetGameInfo(GamesList games, int gameId)
        {
            // Check game start time
            var game = games.Games.FirstOrDefault(x => x.Number == gameId);
            if (game == null)
            {
                return $"Game with number {gameId} not found!";
            }

            if (game.IsPlayed)
            {
                return $"Game #{game.Number} \"{game.WhiteName}\" vs \"{game.BlackName}\" finished with result \"{game.Result}\" for {game.Duration:hh\\:mm\\:ss}";
            }

            if (game.Started.HasValue)
            {
                return $"Game #{game.Number} \"{game.WhiteName}\" vs \"{game.BlackName}\" started at {game.Started:R}";
            }

            var remainingTime = (gameId - games.CountPlayed - 1) * (games.AverageGameTime + new TimeSpan(0, 0, 1, 0)); // +1 minute between games
            var estimatedStartTime = games.LastStarted + remainingTime;
            return $"Game #{game.Number} \"{game.WhiteName}\" vs \"{game.BlackName}\" is estimated to start on {estimatedStartTime:R}";
        }

        private static string GetRemainingDivisionTime(GamesList games)
        {
            var remainingTime = (games.Count - games.CountPlayed) * (games.AverageGameTime + new TimeSpan(0, 0, 1, 0)); // +1 minute between games
            var estimatedEndTime = games.LastStarted + remainingTime;
            var endingS = games.Count - games.CountPlayed != 1 ? 's' : '\0';
            var longestGame = games.Games.Where(x => x.IsPlayed).OrderByDescending(x => x.Duration).FirstOrDefault()?.Duration;
            var shortestGame = games.Games.Where(x => x.IsPlayed).OrderBy(x => x.Duration).FirstOrDefault()?.Duration;
            return $"{games.Count - games.CountPlayed} game{endingS} left. Average duration: {games.AverageGameTime:hh\\:mm\\:ss}. Estimated division end: {estimatedEndTime:R}. Shortest game: {shortestGame:hh\\:mm\\:ss}. Longest game: {longestGame:hh\\:mm\\:ss}";
        }
    }
}
