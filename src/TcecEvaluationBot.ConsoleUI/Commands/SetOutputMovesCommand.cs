namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class SetOutputMovesCommand : BaseCommand
    {
        private readonly bool value;

        public SetOutputMovesCommand(TwitchClient twitchClient, Options options, Settings settings, bool value)
            : base(twitchClient, options, settings)
        {
            this.value = value;
        }

        public override string Execute(string message)
        {
            this.Settings.OutputMoves = this.value;
            return $"OutputMoves set to \"{this.value}\"";
        }
    }
}
