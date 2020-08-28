namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using Newtonsoft.Json.Linq;

    public class RootPosition
    {
        public RootPosition(string fen)
        {
            this.Fen = fen;
            this.Move = Optional<string>.CreateEmpty();
        }

        public RootPosition(string fen, Optional<string> move)
        {
            this.Fen = fen;
            this.Move = move;
        }

        public string Fen { get; set; }

        public Optional<string> Move { get; set; }

        public static RootPosition FromJson(JObject json)
        {
            return new RootPosition(
                json["fen"].Value<string>(),
                json.ContainsKey("move") ? Optional<string>.Create(json["move"].Value<string>()) : Optional<string>.CreateEmpty());
        }
    }
}
