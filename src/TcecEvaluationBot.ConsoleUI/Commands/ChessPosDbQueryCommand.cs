namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.Json;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using RestrictedProcess.Process;
    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery;
    using TcecEvaluationBot.ConsoleUI.Settings;
    using TwitchLib.Client;

    public class ChessPosDbQueryCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;
        private readonly ChessPosDbProxy database;

        public ChessPosDbQueryCommand(TwitchClient twitchClient, Options options, Settings settings, string ip, int port, string path)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(ip))
            {
                this.database = null;
            }
            else
            {
                try
                {
                    this.database = new ChessPosDbProxy(ip, port);
                    this.database.Open(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    this.database = null;
                }
            }

            if (this.database != null && this.database.IsOpen)
            {
                Console.WriteLine($"Exposing database at: {this.database.Path}");
            }
            else
            {
                Console.WriteLine($"Database at {path} couldn't be opened.");
            }
        }

        public override string Execute(string message)
        {
            if (this.database == null || !this.database.IsOpen)
            {
                return "No database open.";
            }

            var fen = this.currentGameInfoProvider.GetInfo().Fen;
            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game?";
            }

            try
            {
                var queryResult = this.database.Query(fen);

                return this.GetResponseStringFromQueryResult(queryResult);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "An error occured while performing a query.";
            }
        }

        public override void Dispose()
        {
            if (this.database != null)
            {
                if (this.database.IsOpen)
                {
                    this.database.Close();
                }

                this.database.Dispose();
            }
        }

        private string GetResponseStringFromQueryResult(QueryResponse result)
        {
            const int numDisplayedChildren = 3;

            var allGameLevels = new List<GameLevel> { GameLevel.Engine, GameLevel.Human, GameLevel.Server };

            var continuations = result.Results.First().ResultsBySelect[Select.Continuations];
            var root = continuations.Root;
            var children = continuations.Children;

            var aggregatedRoot = new AggregatedEntry(root, allGameLevels);
            var aggregatedChildren = new Dictionary<string, AggregatedEntry>();
            foreach ((string move, var e) in children)
            {
                aggregatedChildren.Add(move, new AggregatedEntry(e, allGameLevels));
            }

            var sb = new StringBuilder();
            sb.Append(aggregatedRoot.ToString());

            var bestChildren = this.GetBestChildren(aggregatedChildren, numDisplayedChildren);
            foreach ((string move, var entry) in bestChildren)
            {
                sb.Append(" • ");
                sb.Append(move);
                sb.Append(" ");
                sb.Append(entry.ToString());
            }

            return sb.ToString();
        }

        private List<KeyValuePair<string, AggregatedEntry>> GetBestChildren(Dictionary<string, AggregatedEntry> aggregatedChildren, int maxChildren)
        {
            return aggregatedChildren
                .OrderByDescending(kv => kv.Value.Count)
                .Take(maxChildren)
                .ToList();
        }
    }
}
