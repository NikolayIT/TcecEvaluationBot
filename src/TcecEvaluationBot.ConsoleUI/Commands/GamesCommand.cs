namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models;

    public class GamesCommand : ICommand
    {
        private readonly Options options;

        private readonly GamesInfoProvider gamesInfoProvider;

        private DateTime lastMessage = DateTime.UtcNow.AddDays(-1);

        public GamesCommand(Options options)
        {
            this.options = options;
            this.gamesInfoProvider = new GamesInfoProvider();
        }

        public string Execute(string message)
        {
            if ((DateTime.UtcNow - this.lastMessage).TotalSeconds < this.options.CooldownTime)
            {
                var cooldownRemaining = this.options.CooldownTime - (DateTime.UtcNow - this.lastMessage).TotalSeconds;
                return $"[{DateTime.UtcNow:HH:mm:ss}] \"games\" will be available in {cooldownRemaining:0.0} sec.";
            }

            this.lastMessage = DateTime.UtcNow;

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
            if (messageParts.Length == 1)
            {
                return this.GetAllStats();
            }
            else
            {
                return this.GetEngineStats(messageParts.Skip(1));
            }
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
                return $"[{DateTime.UtcNow:HH:mm:ss}] Engine with that name was not found!";
            }

            var draws = games.Games.Count(x => x.Result == "1/2 1/2" && (x.WhiteName == engineName || x.BlackName == engineName));
            var wins = games.Games.Count(x => x.WhiteName == engineName && x.Result == "1 0")
                       + games.Games.Count(x => x.BlackName == engineName && x.Result == "0 1");
            var loses = games.Games.Count(x => x.WhiteName == engineName && x.Result == "0 1")
                       + games.Games.Count(x => x.BlackName == engineName && x.Result == "1 0");

            return
                $"[{DateTime.UtcNow:HH:mm:ss}] {wins + draws + loses} played game(s) for \"{engineName}\": W:{wins}/D:{draws}/L:{loses}";
        }

        private string GetAllStats()
        {
            var games = this.gamesInfoProvider.GetGames();
            var draws = games.Games.Count(x => x.Result == "1/2 1/2");
            var whiteWins = games.Games.Count(x => x.Result == "1 0");
            var blackWins = games.Games.Count(x => x.Result == "0 1");

            return
                $"[{DateTime.UtcNow:HH:mm:ss}] {whiteWins + draws + blackWins} played game(s): White:{whiteWins}/Draws:{draws}/Black:{blackWins}";
        }
    }
}
