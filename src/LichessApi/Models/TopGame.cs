namespace LichessApi.Models
{
    public class TopGame
    {
        public string Id { get; set; }

        public string Winner { get; set; }

        public string WinnerFriendlyString =>
            this.Winner == "draw" ? "½-½" :
            this.Winner == "white" ? "1-0" :
            this.Winner == "black" ? "0-1" : this.Winner;

        public string Speed { get; set; }

        public Player White { get; set; }

        public Player Black { get; set; }

        public int Year { get; set; }
    }
}
