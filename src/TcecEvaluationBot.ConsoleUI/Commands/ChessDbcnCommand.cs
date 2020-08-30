namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System.Collections.Generic;
    using System.Linq;

    using ChessDotNet;
    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models;
    using TcecEvaluationBot.ConsoleUI.Settings;
    using TwitchLib.Client;

    public class ChessDbcnCommand : BaseCommand
    {
        private readonly int maxBestScores = 10;

        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        private readonly ChessDbcnScoreProvider chessDbcnScoreProvider;

        public ChessDbcnCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);
            this.chessDbcnScoreProvider = new ChessDbcnScoreProvider();
        }

        public override string Execute(string message)
        {
            string fen = null;
            if (message.Trim().Contains(" "))
            {
                var parts = message.Split(" ", 2);
                fen = parts[1];
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                fen = this.currentGameInfoProvider.GetInfo().Fen;
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game?";
            }

            var playerToMove = fen.Contains(" b ") ? Player.Black : Player.White;

            var allScores = this.chessDbcnScoreProvider.GetScores(fen);

            if (allScores.Count == 0)
            {
                return "Unkown position.";
            }

            var bestScores = this.GetBestScores(allScores, this.maxBestScores, playerToMove);

            return this.FormatScoreList(bestScores);
        }

        private List<KeyValuePair<string, ChessDbcnScore>> GetBestScores(
            Dictionary<string, ChessDbcnScore> allScores,
            int maxCount,
            Player sideToMove)
        {
            var ordered =
                sideToMove == Player.White
                ? allScores.OrderByDescending(v => v.Value.Value)
                : allScores.OrderBy(v => v.Value.Value);

            return ordered
                .Take(maxCount)
                .ToList();
        }

        private string FormatScoreList(List<KeyValuePair<string, ChessDbcnScore>> scores)
        {
            return string.Join(" • ", scores.Select(v => $"{v.Key} {v.Value}"));
        }
    }
}
