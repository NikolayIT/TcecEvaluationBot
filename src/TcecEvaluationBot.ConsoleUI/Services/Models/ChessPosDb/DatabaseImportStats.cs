namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery;

    public class DatabaseImportStats
    {
        public DatabaseImportStats(JObject json)
        {
            this.StatsByLevel = new Dictionary<GameLevel, DatabaseSingleLevelImportStats>
            {
                { GameLevel.Engine, new DatabaseSingleLevelImportStats(json["engine"].Value<JObject>()) },
                { GameLevel.Human, new DatabaseSingleLevelImportStats(json["human"].Value<JObject>()) },
                { GameLevel.Server, new DatabaseSingleLevelImportStats(json["server"].Value<JObject>()) },
            };
        }

        public Dictionary<GameLevel, DatabaseSingleLevelImportStats> StatsByLevel { get; private set; }

        public DatabaseSingleLevelImportStats GetTotal()
        {
            DatabaseSingleLevelImportStats total = new DatabaseSingleLevelImportStats(this.StatsByLevel[GameLevel.Engine]);
            total.Add(this.StatsByLevel[GameLevel.Human]);
            total.Add(this.StatsByLevel[GameLevel.Server]);
            return total;
        }
    }
}
