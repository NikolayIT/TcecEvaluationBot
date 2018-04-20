namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Settings;

    public class EvalHelpCommand : BaseCommand
    {
        private readonly Settings settings;

        private readonly Options options;

        public EvalHelpCommand(Settings settings, Options options)
        {
            this.settings = settings;
            this.options = options;
        }

        public override string Execute(string message)
        {
            return $@"commands: 
!eval {{engine({this.settings.Engines.FirstOrDefault()?.Names.FirstOrDefault()})}} {{time({this.options.DefaultEvaluationTime})}} • 
!db • !static • !time {{#|last|next|reverse}} • !games {{engine}} • !reverse • !rand [min] [max]";
        }
    }
}
