namespace TcecEvaluationBot.Pgn
{
    using System.Collections.Generic;

    public class Game
    {
        public Game()
        {
            this.Moves = new List<Move>();
            this.Tags = new List<Tag>();
        }

        public IList<Move> Moves { get; set; }

        public IList<Tag> Tags { get; set; }
    }
}
