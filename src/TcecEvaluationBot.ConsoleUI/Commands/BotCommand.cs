namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class BotCommand : BaseCommand
    {
        public BotCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
        }

        public override string Execute(string message)
        {
            return "Ready and standby.";
        }
    }
}
