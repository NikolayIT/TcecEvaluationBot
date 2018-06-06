namespace TcecEvaluationBot.ConsoleUI.Settings
{
    using System.Collections.Generic;

    public class EngineSettings
    {
        public IEnumerable<string> Names { get; set; }

        public string Executable { get; set; }

        public string Arguments { get; set; }

        public string Title { get; set; }

        public string PositionEvaluator { get; set; }

        public IEnumerable<string> CommandLineInputs { get; set; }
    }
}
