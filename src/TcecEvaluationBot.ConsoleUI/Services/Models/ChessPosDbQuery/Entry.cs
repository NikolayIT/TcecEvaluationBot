namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using Newtonsoft.Json.Linq;

    public class Entry
    {
        public Entry(ulong count)
        {
            this.Count = count;
        }

        public Entry(ulong count, Optional<GameHeader> firstGame, Optional<GameHeader> lastGame, Optional<long> eloDiff)
        {
            this.Count = count;
            this.FirstGame = firstGame;
            this.LastGame = lastGame;
            this.EloDiff = eloDiff;
        }

        public ulong Count { get; set; }

        public Optional<GameHeader> FirstGame { get; set; }

        public Optional<GameHeader> LastGame { get; set; }

        public Optional<long> EloDiff { get; set; }

        public static Entry FromJson(JObject json)
        {
            return new Entry(
                json["count"].Value<ulong>(),
                json.ContainsKey("first_game") ? Optional<GameHeader>.Create(GameHeader.FromJson(json["first_game"].Value<JObject>())) : Optional<GameHeader>.CreateEmpty(),
                json.ContainsKey("last_game") ? Optional<GameHeader>.Create(GameHeader.FromJson(json["last_game"].Value<JObject>())) : Optional<GameHeader>.CreateEmpty(),
                json.ContainsKey("elo_diff") ? Optional<long>.Create(json["elo_diff"].Value<long>()) : Optional<long>.CreateEmpty());
        }
    }
}
