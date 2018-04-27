namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public abstract class BaseCommand : ICommand
    {
        protected BaseCommand(TwitchClient twitchClient, Options options, Settings settings)
        {
            this.TwitchClient = twitchClient;
            this.Options = options;
            this.Settings = settings;
        }

        protected TwitchClient TwitchClient { get; }

        protected Options Options { get; }

        protected Settings Settings { get; }

        public abstract string Execute(string message);
    }
}
