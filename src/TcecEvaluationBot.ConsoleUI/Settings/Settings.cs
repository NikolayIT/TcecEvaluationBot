namespace TcecEvaluationBot.ConsoleUI.Settings
{
    public class Settings
    {
        public string LivePgnUrl { get; set; }

        public string ScheduleUrl { get; set; }

        public string LichessDbUrl { get; set; }

        public EngineSettings[] Engines { get; set; }
    }
}
