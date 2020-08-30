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

    public class DatabaseInfo
    {
        public DatabaseInfo(string path, bool isOpen)
        {
            this.Path = path;
            this.IsOpen = isOpen;
            this.Stats = null;
        }

        public bool IsOpen { get; private set; }

        public string Path { get; private set; }

        public DatabaseStats Stats { get; private set; }

        public void SetStatsFromJson(JObject json)
        {
            this.Stats = new DatabaseStats(json);
        }
    }
}
