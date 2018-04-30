namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System.Text;

    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class EvalEnginesCommand : BaseCommand
    {
        public EvalEnginesCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
        }

        public override string Execute(string message)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("engines: ");
            foreach (var engineSetting in this.Settings.Engines)
            {
                var name = engineSetting.Title.Replace("_", " ");
                if (name.Contains(", Courtesy"))
                {
                    name = name.Split(", Courtesy")[0];
                }

                stringBuilder.Append(name + " • ");
            }

            return stringBuilder.ToString().Trim(' ', '•');
        }
    }
}
