namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Linq;
    using System.Text;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;
    using TcecEvaluationBot.Pgn;

    using TwitchLib.Client;

    public class ReverseCommand : BaseCommand
    {
        private readonly ArchiveInfoProvider archiveInfoProvider;

        public ReverseCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.archiveInfoProvider = new ArchiveInfoProvider(settings.LivePgnUrl, settings.ArchivePgnUrl);
        }

        public override string Execute(string message)
        {
            var gamesList = this.archiveInfoProvider.GetGames();
            if (!gamesList.Games.Any())
            {
                return "No games played.";
            }

            var currentGame = this.archiveInfoProvider.GetCurrentGame();

            var countOfTheSameGame = gamesList.Games.Count(
                x => (x.White == currentGame.White && x.Black == currentGame.Black)
                     || (x.White == currentGame.Black && x.Black == currentGame.White));
            if (countOfTheSameGame % 2 == 0)
            {
                return "Reverse game not found.";
            }

            var reverseGame = gamesList.Games.LastOrDefault(x => x.White == currentGame.Black && x.Black == currentGame.White);
            if (reverseGame == null)
            {
                return "Reverse game not found.";
            }

            return $"Game #{reverseGame.Id} \"{reverseGame.White}\" vs. \"{reverseGame.Black}\": {this.ToShortNotation(reverseGame)}";
        }

        public string ToShortNotation(Game game, int firstNPlies = 30)
        {
            var movesBuilder = new StringBuilder();

            if (game.Moves.Any(x => x.Comment.StartsWith("book")))
            {
                movesBuilder.Append("[book] ");
            }

            var moves = game.Moves.Where(x => !x.Comment.StartsWith("book")).ToList();
            for (var index = 0; index < Math.Min(moves.Count, firstNPlies); index++)
            {
                var move = moves[index];
                if (index == 0 && move.Color == Color.Black)
                {
                    movesBuilder.Append($"{move.Number}... ");
                }

                if (move.Color == Color.White)
                {
                    movesBuilder.Append($"{move.Number}. ");
                }

                movesBuilder.Append($"{move.San} ");
            }

            if (moves.Count > firstNPlies)
            {
                movesBuilder.Append("... ");
            }

            movesBuilder.Append(game.Result);

            return movesBuilder.ToString();
        }
    }
}
