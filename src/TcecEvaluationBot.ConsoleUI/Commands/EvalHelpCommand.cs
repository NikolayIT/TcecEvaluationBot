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
            return $@"commands: !eval {{engine({this.Settings.Engines.FirstOrDefault()?.Names.FirstOrDefault()})}} {{time({this.Options.DefaultEvaluationTime})}} •
!static • !links {{fen}} • !db {{fen}} • !idb {{fen}} • !cdb {{fen}} • !tb {{fen}} • !time {{#|last|next|reverse}} • !games {{engine}} • !reverse •
!rand [min] [max] • !calc [expression] • !define [word] • !urban [word]• !chesswiki [term] • !evalhelp • !evalengines • !temp • 
!outputmoveson • !outputmovesoff • !bot • <eval_bot {this.environmentInformationProvider.VersionNumber}>";
        }
    }
}
