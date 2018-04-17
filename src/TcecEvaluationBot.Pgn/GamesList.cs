namespace TcecEvaluationBot.Pgn
{
    using System.Collections.Generic;

    public class GamesList
    {
        public GamesList(IEnumerable<Game> games)
        {
            this.Games = games;
        }

        public IEnumerable<Game> Games { get; set; }
    }
}
