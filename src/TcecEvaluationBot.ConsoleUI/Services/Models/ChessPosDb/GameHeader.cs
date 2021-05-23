namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Text;

    using Newtonsoft.Json.Linq;

    public class GameHeader
    {
        public GameHeader(
            uint gameId,
            GameResult result,
            Date date,
            Eco eco,
            Optional<ushort> plyCount,
            string @event,
            string white,
            string black)
        {
            this.GameId = gameId;
            this.Result = result;
            this.Date = date;
            this.Eco = eco;
            this.PlyCount = plyCount;
            this.Event = @event;
            this.White = white;
            this.Black = black;
        }

        public uint GameId { get; set; }

        public GameResult Result { get; set; }

        public Date Date { get; set; }

        public Eco Eco { get; set; }

        public Optional<ushort> PlyCount { get; set; }

        public string Event { get; set; }

        public string White { get; set; }

        public string Black { get; set; }

        public static GameHeader FromJson(JObject json)
        {
            var plyCount =
                json.ContainsKey("ply_count")
                    ? Optional<ushort>.Create(json["ply_count"].Value<ushort>())
                    : Optional<ushort>.CreateEmpty();

            return new GameHeader(
                json["game_id"].Value<uint>(),
                GameResultHelper.FromStringPgnFormat(json["result"].Value<string>()).First(),
                Date.FromJson(json["date"]),
                Eco.FromJson(json["eco"]),
                plyCount,
                json["event"].Value<string>(),
                json["white"].Value<string>(),
                json["black"].Value<string>());
        }

        public bool IsBefore(GameHeader gameHeader)
        {
            if (this.Date.IsBefore(gameHeader.Date))
            {
                return true;
            }

            if (gameHeader.Date.IsBefore(this.Date))
            {
                return false;
            }

            return this.GameId < gameHeader.GameId;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{this.White} - {this.Black} ");
            sb.Append(this.Result.ToStringPgnUnicodeFormat());
            sb.Append(" ");
            sb.Append(this.Date.ToStringYear());

            return sb.ToString();
        }
    }
}
