namespace TcecEvaluationBot.ConsoleUI.Settings
{
    public class Settings
    {
        public string LivePgnUrl { get; set; }

        public string ScheduleUrl { get; set; }

        public string ArchivePgnUrl { get; set; }

        public bool OutputMoves { get; set; }

        public EngineSettings[] Engines { get; set; }

        public string IccfDatabasePath { get; set; }

        public string IccfDatabaseIp { get; set; }

        public int IccfDatabasePort { get; set; }

        public string OxfordApiAppId { get; set; }

        public string OxfordApiAppKey { get; set; }
    }
}
