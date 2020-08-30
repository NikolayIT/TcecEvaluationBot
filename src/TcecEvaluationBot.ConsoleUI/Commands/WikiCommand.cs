namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class WikiCommand : BaseCommand
    {
        private readonly ChessProgrammingPageContentProvider chessProgrammingPageContentProvider;

        public WikiCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.chessProgrammingPageContentProvider = new ChessProgrammingPageContentProvider();
        }

        public override string Execute(string message)
        {
            var parts = message.Split(' ', 2);
            if (parts.Length != 2)
            {
                return "Usage: !chesswiki [word]";
            }

            var word = parts[1];
            var meaning = this.chessProgrammingPageContentProvider.GetContent(word).GetAwaiter().GetResult();
            return meaning == null
                       ? $"\"{word}\" not found <https://chessprogramming.org/{word.Replace(" ", "_")}>"
                       : $"{meaning} <https://chessprogramming.org/{word.Replace(" ", "_")}>";
        }
    }
}
