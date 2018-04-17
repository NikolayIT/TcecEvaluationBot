namespace TcecEvaluationBot.Pgn
{
    using System.Collections.Generic;
    using System.Linq;

    public class Game
    {
        public Game()
        {
            this.Moves = new List<Move>();
            this.Tags = new List<Tag>();
        }

        public int Id { get; set; }

        public IList<Move> Moves { get; set; }

        public IList<Tag> Tags { get; set; }

        public string Result => this.Tags.FirstOrDefault(x => x.Name == "Result")?.Value;

        public string White => this.Tags.FirstOrDefault(x => x.Name == "White")?.Value;

        public string Black => this.Tags.FirstOrDefault(x => x.Name == "Black")?.Value;
    }
}
