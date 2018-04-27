namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class EvalHelpCommand : BaseCommand
    {
        public EvalHelpCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
        }

        public override string Execute(string message)
        {
            return $@"commands: 
!eval {{engine({this.Settings.Engines.FirstOrDefault()?.Names.FirstOrDefault()})}} {{time({this.Options.DefaultEvaluationTime})}} • 
!db • !static • !time {{#|last|next|reverse}} • !games {{engine}} • !reverse • !rand [min] [max]";
        }
    }
}
