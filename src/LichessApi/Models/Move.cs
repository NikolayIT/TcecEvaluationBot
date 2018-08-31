namespace LichessApi.Models
{
    public class Move
    {
        public string Uci { get; set; }

        public string San { get; set; }

        public int White { get; set; }

        public int Draws { get; set; }

        public int Black { get; set; }

        public int AverageRating { get; set; }
    }
}
