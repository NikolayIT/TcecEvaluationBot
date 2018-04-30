namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class EvalHelpCommand : BaseCommand
    {
        private readonly EnvironmentInformationProvider environmentInformationProvider;

        public EvalHelpCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.environmentInformationProvider = new EnvironmentInformationProvider();
        }

        public override string Execute(string message)
        {
            return $@"commands: 
!eval {{engine({this.Settings.Engines.FirstOrDefault()?.Names.FirstOrDefault()})}} {{time({this.Options.DefaultEvaluationTime})}} • 
!db • !static • !time {{#|last|next|reverse}} • !games {{engine}} • !reverse • !rand [min] [max] • evalhelp • evalengines •
<eval_bot {this.environmentInformationProvider.VersionNumber}>";
        }
    }
}
