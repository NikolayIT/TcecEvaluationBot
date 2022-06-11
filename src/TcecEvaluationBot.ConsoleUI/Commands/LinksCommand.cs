namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Text;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class LinksCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        public LinksCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);
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
                fen = this.currentGameInfoProvider.GetInfo().Fen;
                sb.Append($"({fen.GetMoveInfoFromFen()}) ");
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game or invalid FEN?";
            }

            sb.Append($"Lichess: https://lichess.org/analysis/standard/{Uri.EscapeDataString(fen)} • ");
            sb.Append($"Syzygy: https://syzygy-tables.info/?fen={Uri.EscapeDataString(fen)}");

            return sb.ToString();
        }
    }
}
