namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models;

    public class TimeCommand : ICommand
    {
        private readonly GamesInfoProvider gamesInfoProvider;

        public TimeCommand()
        {
            this.gamesInfoProvider = new GamesInfoProvider();
        }

        public string Execute(string message)
        {
            GamesList games;
            try
            {
                games = this.gamesInfoProvider.GetGames();
            }
            catch (Exception)
            {
                return $"[{DateTime.UtcNow:HH:mm:ss}] Unable to retrieve schedule data.";
            }

            if (games.CountPlayed == 0)
            {
                return $"[{DateTime.UtcNow:HH:mm:ss}] No games played.";
            }

            var messageParts = message.Split(" ");
            var gameId = messageParts.Any(x => int.TryParse(x, out _))
                             ? int.Parse(messageParts.FirstOrDefault(x => int.TryParse(x, out _)))
                             : (int?)null;
            if (gameId.HasValue && gameId >= 1 && gameId <= games.Count)
            {
                return GetGameInfo(games, gameId.Value);
            }
            else
            {
                return GetRemainingDivisionTime(games);
            }
        }

        private static string GetGameInfo(GamesList games, int gameId)
        {
            // Check game start time
            var game = games.Games.FirstOrDefault(x => x.Number == gameId);
            if (game == null)
            {
                return $"[{DateTime.UtcNow:HH:mm:ss}] Game with number {gameId} not found!";
            }

            if (game.IsPlayed)
            {
                return $"[{DateTime.UtcNow:HH:mm:ss}] Game \"{game.WhiteName}\" vs \"{game.BlackName}\" finished with result \"{game.Result}\"";
            }

            if (game.Started.HasValue)
            {
                return $"[{DateTime.UtcNow:HH:mm:ss}] Game \"{game.WhiteName}\" vs \"{game.BlackName}\" started at {game.Started:R}";
            }

            var estimatedStartTime = games.LastStarted
                                     + ((gameId - games.CountPlayed - 1)
                                        * (games.AverageGameTime + new TimeSpan(0, 0, 1, 0))); // +1 minute between games
            return $"[{DateTime.UtcNow:HH:mm:ss}] Game \"{game.WhiteName}\" vs \"{game.BlackName}\" is estimated to start on {estimatedStartTime:R}";
        }

        private static string GetRemainingDivisionTime(GamesList games)
        {
            var estimatedEndTime = games.LastStarted
                                   + ((games.Count - games.CountPlayed)
                                      * (games.AverageGameTime + new TimeSpan(0, 0, 1, 0))); // +1 minute between games
            return $"[{DateTime.UtcNow:HH:mm:ss}] {games.Count - games.CountPlayed} games left. Average duration: {games.AverageGameTime:hh\\:mm\\:ss}. Estimated division end: {estimatedEndTime:R}.";
        }
    }
}
