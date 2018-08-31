namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System.Text;

    using LichessApi;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class TbCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        private readonly ILichessApiClient lichessApiClient;

        public TbCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);
            this.lichessApiClient = new LichessApiClient();
        }

        public override string Execute(string message)
        {
            var sb = new StringBuilder();
            string fen = null;
            if (message.Contains(" "))
            {
                var parts = message.Split(" ", 2);
                fen = parts[1];
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                fen = this.currentGameInfoProvider.GetFen();
                sb.Append($"({fen.GetMoveInfoFromFen()}) ");
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game or invalid FEN?";
            }

            var tablebaseInfo = this.lichessApiClient.GetTablebaseInfo(fen);
            if (tablebaseInfo == null)
            {
                return $"Invalid FEN: \"{fen}\" or Lichess down.";
            }

            // Current position
            if (tablebaseInfo.Wdl.HasValue || tablebaseInfo.Dtz.HasValue || tablebaseInfo.Dtm.HasValue)
            {
                sb.Append(
                    fen.GetPlayerToMoveFromFen() == "w"
                        ? $"WDL:{tablebaseInfo.Wdl}; DTZ:{tablebaseInfo.Dtz}; DTM:{tablebaseInfo.Dtm} • "
                        : $"WDL:{-tablebaseInfo.Wdl}; DTZ:{-tablebaseInfo.Dtz}; DTM:{-tablebaseInfo.Dtm} • ");
            }
            else
            {
                sb.Append("Current position not found in 7-men TB • ");
            }

            // Possible moves
            if (tablebaseInfo.Moves.Length == 0)
            {
                sb.Append("No possible moves • ");
            }
            else
            {
                foreach (var move in tablebaseInfo.Moves)
                {
                    sb.Append(move.San);
                    if (move.Wdl.HasValue || move.Dtz.HasValue || move.Dtm.HasValue)
                    {
                        sb.Append(
                            fen.GetPlayerToMoveFromFen() == "w"
                                ? $" ({-move.Wdl};{-move.Dtz};{-move.Dtm})"
                                : $" ({move.Wdl};{move.Dtz};{move.Dtm})");
                    }

                    sb.Append(" • ");
                }
            }

            return sb.ToString().Trim('-', ' ') + " <Lichess>";
        }
    }
}
