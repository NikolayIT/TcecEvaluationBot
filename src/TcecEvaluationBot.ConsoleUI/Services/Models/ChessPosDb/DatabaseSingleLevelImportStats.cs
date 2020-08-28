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

    public class DatabaseSingleLevelImportStats : DatabaseSingleLevelStats
    {
        public DatabaseSingleLevelImportStats(JObject json)
            : base(json)
        {
            this.NumSkippedGames = json["num_skipped_games"].Value<ulong>();
        }

        public DatabaseSingleLevelImportStats(DatabaseSingleLevelImportStats other)
            : base(other)
        {
            this.NumSkippedGames = other.NumSkippedGames;
        }

        public ulong NumSkippedGames { get; private set; }

        public void Add(DatabaseSingleLevelImportStats other)
        {
            base.Add(other);
            this.NumSkippedGames += other.NumSkippedGames;
        }
    }
}
