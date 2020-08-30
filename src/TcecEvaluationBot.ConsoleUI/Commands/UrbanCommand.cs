namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class UrbanCommand : BaseCommand
    {
        private readonly UrbanDictionaryDefinitionsProvider urbanDictionaryDefinitionsProvider;

        public UrbanCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.urbanDictionaryDefinitionsProvider = new UrbanDictionaryDefinitionsProvider();
        }

        public override string Execute(string message)
        {
            var parts = message.Split(' ', 2);
            if (parts.Length != 2)
            {
                return "Usage: !urban [word]";
            }

            var word = parts[1];
            var meaning = this.urbanDictionaryDefinitionsProvider.GetWordDefinition(word).GetAwaiter().GetResult();
            return $"{word}: {meaning} <https://urbandictionary.com/define.php?term={word.Replace(" ", "%20")}>";
        }
    }
}
