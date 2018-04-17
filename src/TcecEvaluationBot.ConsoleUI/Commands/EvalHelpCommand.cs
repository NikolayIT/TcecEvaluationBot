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
            return $@"Commands: 
!eval {{engine({this.settings.Engines.FirstOrDefault()?.Name})}} {{time({this.options.DefaultEvaluationTime})}} • 
!time {{gameNum|last|next}} • 
!games {{engine}} • 
!rand [min] [max] • 
!db • 
!static • 
!reverse";
        }
    }
}
