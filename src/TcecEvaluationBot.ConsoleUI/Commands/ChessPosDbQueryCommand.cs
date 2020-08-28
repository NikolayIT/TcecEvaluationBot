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

    using TcecEvaluationBot.ConsoleUI.Settings;

    using TcecEvaluationBot.ConsoleUI.Services;

    using TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery;

    using TwitchLib.Client;

    class ChessPosDbQueryCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;
        private readonly ChessPosDbProxy database;

        public ChessPosDbQueryCommand(TwitchClient twitchClient, Options options, Settings settings, string ip, int port, string path)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(ip))
            {
                database = null;
            }
            else
            {
                try
                {
                    database = new ChessPosDbProxy(ip, port);
                    database.Open(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    database = null;
                }
            }
        }

        public override string Execute(string message)
        {
            if (database == null || !database.IsOpen)
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
                var queryResult = database.Query(fen);

                return GetResponseStringFromQueryResult(queryResult);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "An error occured while performing a query.";
            }
        }

        public override void Dispose()
        {
            if (database != null)
            {
                if (database.IsOpen)
                {
                    database.Close();
                }

                database.Dispose();
            }
        }

        private string GetResponseStringFromQueryResult(QueryResponse result)
        {
            const int numDisplayedChildren = 3;

            var allGameLevels = new List<GameLevel>{ GameLevel.Engine, GameLevel.Human, GameLevel.Server };

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

            var bestChildren = GetBestChildren(aggregatedChildren, numDisplayedChildren);
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
