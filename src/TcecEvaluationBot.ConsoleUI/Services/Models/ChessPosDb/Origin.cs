namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    internal struct Origin
    {
        public Origin(GameLevel level, GameResult result)
        {
            this.Level = level;
            this.Result = result;
        }

        public GameLevel Level { get; set; }

        public GameResult Result { get; set; }
    }
}
