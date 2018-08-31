namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Text;

    using LichessApi;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class DbCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        private readonly ILichessApiClient lichessApiClient;

        public DbCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);
            this.lichessApiClient = new LichessApiClient();
        }

        public override string Execute(string message)
        {
            var fen = this.currentGameInfoProvider.GetFen();
            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game?";
            }

            var positionInfo = this.lichessApiClient.GetPositionInfo(fen);
            var sb = new StringBuilder();

            sb.Append($"({fen.GetMoveInfoFromFen()}) ");

            // Stats
            sb.Append($"+{positionInfo.White}={positionInfo.Draws}-{positionInfo.Black} • ");

            // Moves
            if (positionInfo.Moves.Length == 0)
            {
                sb.Append("No moves • ");
            }
            else
            {
                for (var i = 0; i < Math.Min(positionInfo.Moves.Length, 3); i++)
                {
                    var move = positionInfo.Moves[i];
                    sb.Append($"{move.San} (+{move.White}={move.Draws}-{move.Black}) • ");
                }
            }

            // Games
            if (positionInfo.TopGames.Length == 0)
            {
                sb.Append("No games • ");
            }
            else
            {
                for (var i = 0; i < Math.Min(positionInfo.TopGames.Length, 2); i++)
                {
                    var topGame = positionInfo.TopGames[i];
                    sb.Append($"[{topGame.Year}] {topGame.White.Name} ({topGame.White.Rating}) vs {topGame.Black.Name} ({topGame.Black.Rating}): {topGame.Winner} • ");
                }
            }

            return sb.ToString().Trim('-', ' ') + " <Lichess>";
        }
    }
}
