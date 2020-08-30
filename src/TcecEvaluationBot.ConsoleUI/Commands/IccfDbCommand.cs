namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class IccfDbCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        private readonly ChessPosDbProxy database;

        public IccfDbCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);

            if (string.IsNullOrWhiteSpace(settings.IccfDatabasePath) || string.IsNullOrWhiteSpace(settings.IccfDatabaseIp))
            {
                this.database = null;
            }
            else
            {
                try
                {
                    this.database = new ChessPosDbProxy(settings.IccfDatabaseIp, settings.IccfDatabasePort);
                    this.database.Open(settings.IccfDatabasePath);
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
                Console.WriteLine($"Database at \"{settings.IccfDatabasePath}\" couldn't be opened.");
            }
        }

        public override string Execute(string message)
        {
            if (this.database == null || !this.database.IsOpen)
            {
                return "No database open.";
            }

            string fen = null;
            if (message.Trim().Contains(" "))
            {
                var parts = message.Split(" ", 2);
                fen = parts[1];
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                fen = this.currentGameInfoProvider.GetInfo().Fen;
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game?";
            }

            try
            {
                var queryResult = this.database.Query(fen);

                return message.Trim().Contains(" ")
                           ? this.GetResponseStringFromQueryResult(queryResult)
                           : $"({fen.GetMoveInfoFromFen()}) " + this.GetResponseStringFromQueryResult(queryResult);
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
            const int numDisplayedChildren = 5;

            var allGameLevels = new List<GameLevel> { GameLevel.Engine, GameLevel.Human, GameLevel.Server };

            var continuations = result.Results.First().ResultsBySelect[Select.Continuations];
            var transpositions = result.Results.First().ResultsBySelect[Select.Transpositions];
            var rootContinuations = continuations.Root;
            var rootTranspositions = transpositions.Root;
            var childrenContinuations = continuations.Children;
            var childrenTranspositions = transpositions.Children;

            var aggregatedRoot = new AggregatedEntry(rootContinuations, allGameLevels);
            aggregatedRoot.Combine(new AggregatedEntry(rootTranspositions, allGameLevels));

            var aggregatedChildren = new Dictionary<string, AggregatedEntry>();
            foreach (var (move, e) in childrenTranspositions)
            {
                aggregatedChildren.Add(move, new AggregatedEntry(e, allGameLevels));
            }

            var aggregatedChildrenContinuations = new Dictionary<string, AggregatedEntry>();
            foreach (var (move, e) in childrenContinuations)
            {
                var ae = new AggregatedEntry(e, allGameLevels);

                aggregatedChildrenContinuations.Add(move, ae);

                if (aggregatedChildren.ContainsKey(move))
                {
                    aggregatedChildren[move].Combine(ae);
                }
                else
                {
                    aggregatedChildren.Add(move, ae);
                }
            }

            var onlyTranspositions = childrenTranspositions.Keys.Where(
                move => aggregatedChildrenContinuations.Any(c => c.Key == move && c.Value.Count == 0));

            var sb = new StringBuilder();
            sb.Append(aggregatedRoot);

            var bestChildren = this.GetBestChildren(aggregatedChildren, numDisplayedChildren);
            foreach (var (move, entry) in bestChildren)
            {
                sb.Append(" • ");

                if (onlyTranspositions.Contains(move))
                {
                    sb.Append("(T) ");
                }

                sb.Append(move);
                sb.Append(" ");
                sb.Append(entry);
            }

            sb.Append(" <ICCF>");
            return sb.ToString();
        }

        private List<KeyValuePair<string, AggregatedEntry>> GetBestChildren(Dictionary<string, AggregatedEntry> aggregatedChildren, int maxChildren)
        {
            return aggregatedChildren
                .OrderByDescending(kv => kv.Value.Count)
                .Where(x => x.Value.Count > 0)
                .Take(maxChildren)
                .ToList();
        }
    }
}
