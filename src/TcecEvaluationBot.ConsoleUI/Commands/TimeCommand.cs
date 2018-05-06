namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class TimeCommand : BaseCommand
    {
        private readonly GamesInfoProvider gamesInfoProvider;

        public TimeCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
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

            var messageParts = message.Split(" ").Select(x => x.ToLower()).ToList();
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

            if (messageParts.Contains("reverse"))
            {
                return GetReverseGameInfo(games);
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

        private static string GetReverseGameInfo(GamesList games)
        {
            var currentGame = games.Games.OrderBy(x => x.Number).FirstOrDefault(x => !x.IsPlayed);
            if (currentGame == null)
            {
                return "No active game?";
            }

            var sameGamesCount = games.Games.Count(
                x => ((x.WhiteName == currentGame.WhiteName && x.BlackName == currentGame.BlackName)
                     || (x.WhiteName == currentGame.BlackName && x.BlackName == currentGame.WhiteName)) && x.IsPlayed);
            if (sameGamesCount % 2 == 0)
            {
                var nextGame = games.Games.OrderBy(x => x.Number).Where(
                        x => (x.WhiteName == currentGame.WhiteName && x.BlackName == currentGame.BlackName)
                             || (x.WhiteName == currentGame.BlackName && x.BlackName == currentGame.WhiteName))
                    .FirstOrDefault(x => !x.Started.HasValue);
                return nextGame == null ? "Next reverse game not found." : GetGameInfo(games, nextGame.Number);
            }
            else
            {
                var previousGame = games.Games.OrderBy(x => x.Number).Where(
                        x => (x.WhiteName == currentGame.WhiteName && x.BlackName == currentGame.BlackName)
                             || (x.WhiteName == currentGame.BlackName && x.BlackName == currentGame.WhiteName))
                    .LastOrDefault(x => x.IsPlayed);
                return previousGame == null ? "Previous reverse game not found." : GetGameInfo(games, previousGame.Number);
            }
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
                return $"Game #{game.Number} \"{game.WhiteName}\" vs \"{game.BlackName}\" finished with result \"{game.Result}\" for {game.Duration:hh\\:mm\\:ss} on {game.Started + game.Duration:R}";
            }

            if (game.Started.HasValue)
            {
                return $"Game #{game.Number} \"{game.WhiteName}\" vs \"{game.BlackName}\" started at {game.Started:R}";
            }

            var remainingTime = (gameId - games.CountPlayed - 1) * (games.AverageGameTime + new TimeSpan(0, 0, 1, 0)); // +1 minute between games
            var estimatedStartTime = games.LastStarted + remainingTime;
            var timeRemaining = estimatedStartTime - DateTime.UtcNow;
            return $"Game #{game.Number} \"{game.WhiteName}\" vs \"{game.BlackName}\" is estimated to start on {estimatedStartTime:R} (After {(int)timeRemaining.TotalHours}h {timeRemaining.Minutes}')";
        }

        private static string GetRemainingDivisionTime(GamesList games)
        {
            var gamesCount = games.Count;
            var remainingTime = (gamesCount - games.CountPlayed) * (games.AverageGameTime + new TimeSpan(0, 0, 1, 0)); // +1 minute between games
            var estimatedEndTime = games.LastStarted + remainingTime;
            var endingS = gamesCount - games.CountPlayed != 1 ? 's' : '\0';
            var longestGame = games.Games.Where(x => x.IsPlayed).OrderByDescending(x => x.Duration).FirstOrDefault()?.Duration;
            var shortestGame = games.Games.Where(x => x.IsPlayed).OrderBy(x => x.Duration).FirstOrDefault()?.Duration;
            return $"{gamesCount - games.CountPlayed}/{gamesCount} game{endingS} left • Average duration: {games.AverageGameTime:hh\\:mm\\:ss} • Estimated division end: {estimatedEndTime:R} • Shortest game: {shortestGame:hh\\:mm\\:ss} • Longest game: {longestGame:hh\\:mm\\:ss}";
        }
    }
}
