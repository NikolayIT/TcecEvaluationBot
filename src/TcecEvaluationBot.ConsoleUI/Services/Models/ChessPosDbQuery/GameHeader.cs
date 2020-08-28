using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public class GameHeader
    {
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
            return new GameHeader(
                json["game_id"].Value<uint>(),
                GameResultHelper.FromStringPgnFormat(json["result"].Value<string>()).First(),
                Date.FromJson(json["date"]),
                Eco.FromJson(json["eco"]),
                json.ContainsKey("ply_count") 
                    ? Optional<ushort>.Create(json["ply_count"].Value<ushort>()) 
                    : Optional<ushort>.CreateEmpty(),
                json["event"].Value<string>(),
                json["white"].Value<string>(),
                json["black"].Value<string>()
                );
        }

        public GameHeader(
            uint gameId,
            GameResult result,
            Date date,
            Eco eco,
            Optional<ushort> plyCount,
            string @event,
            string white,
            string black
            )
        {
            GameId = gameId;
            Result = result;
            Date = date;
            Eco = eco;
            PlyCount = plyCount;
            Event = @event;
            White = white;
            Black = black;
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

            return GameId < gameHeader.GameId;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{White} - {Black} ");
            sb.Append(Result.ToStringPgnUnicodeFormat());
            sb.Append(" ");
            sb.Append(Date.ToStringOmitUnknown());

            return sb.ToString();
        }
    }
}
