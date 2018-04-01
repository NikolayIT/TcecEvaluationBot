namespace TcecEvaluationBot.ConsoleUI.Services.LichessModels
{
    public class TopGame
    {
        public string Id { get; set; }

        public string Winner { get; set; }

        public string Speed { get; set; }

        public Player White { get; set; }

        public Player Black { get; set; }

        public int Year { get; set; }
    }
}
