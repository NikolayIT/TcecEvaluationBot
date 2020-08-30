namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class DefineCommand : BaseCommand
    {
        private readonly OxfordDictionaryDefinitionsProvider oxfordDictionaryDefinitionsProvider;

        public DefineCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.oxfordDictionaryDefinitionsProvider = new OxfordDictionaryDefinitionsProvider(
                settings.OxfordApiAppId,
                settings.OxfordApiAppKey);
        }

        public override string Execute(string message)
        {
            var parts = message.Split(' ', 2);
            if (parts.Length != 2)
            {
                return "Usage: !define [word]";
            }

            var word = parts[1];
            var meaning = this.oxfordDictionaryDefinitionsProvider.GetWordDefinition(word).GetAwaiter().GetResult();
            return $"{word}: {meaning} <Oxford> https://en.wiktionary.org/wiki/{word.Replace(" ", "_")}";
        }
    }
}
