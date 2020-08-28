namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;
    using TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery;

    public class DatabaseStats
    {
        public DatabaseStats(JObject json)
        {
            this.StatsByLevel = new Dictionary<GameLevel, DatabaseSingleLevelStats>
            {
                { GameLevel.Engine, new DatabaseSingleLevelStats(json["engine"].Value<JObject>()) },
                { GameLevel.Human, new DatabaseSingleLevelStats(json["human"].Value<JObject>()) },
                { GameLevel.Server, new DatabaseSingleLevelStats(json["server"].Value<JObject>()) },
            };
        }

        public Dictionary<GameLevel, DatabaseSingleLevelStats> StatsByLevel { get; private set; }

        public DatabaseSingleLevelStats GetTotal()
        {
            DatabaseSingleLevelStats total = new DatabaseSingleLevelStats(this.StatsByLevel[GameLevel.Engine]);
            total.Add(this.StatsByLevel[GameLevel.Human]);
            total.Add(this.StatsByLevel[GameLevel.Server]);
            return total;
        }
    }
}
