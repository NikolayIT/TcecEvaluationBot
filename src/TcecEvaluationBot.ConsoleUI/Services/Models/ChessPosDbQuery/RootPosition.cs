
using Newtonsoft.Json.Linq;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public class RootPosition
    {
        public string Fen { get; set; }
        public Optional<string> Move { get; set; }

        public static RootPosition FromJson(JObject json)
        {
            return new RootPosition(
                json["fen"].Value<string>(),
                json.ContainsKey("move") ? Optional<string>.Create(json["move"].Value<string>()) : Optional<string>.CreateEmpty()
            );
        }

        public RootPosition(string fen)
        {
            Fen = fen;
            Move = Optional<string>.CreateEmpty();
        }

        public RootPosition(string fen, Optional<string> move)
        {
            Fen = fen;
            Move = move;
        }
    }
}
