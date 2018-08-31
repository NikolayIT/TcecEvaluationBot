namespace LichessApi.Models
{
    public class DatabasePosition
    {
        public int White { get; set; }

        public int Draws { get; set; }

        public int Black { get; set; }

        public Move[] Moves { get; set; }

        public int AverageRating { get; set; }

        public TopGame[] TopGames { get; set; }
    }
}
