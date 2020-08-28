using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public class ResultForRoot
    {
        public RootPosition Position { get; set; }
        public Dictionary<Select, SelectResult> ResultsBySelect { get; set; }
        public Dictionary<string, SegregatedEntries> Retractions { get; set; }

        public static ResultForRoot FromJson(JObject json)
        {
            var result = new ResultForRoot(
                RootPosition.FromJson(json["position"].Value<JObject>())
            );

            foreach (Select select in SelectHelper.Values)
            {
                var selectStr = select.Stringify();
                if (json.ContainsKey(selectStr))
                {
                    result.ResultsBySelect.Add(select, SelectResult.FromJson(json[selectStr].Value<JObject>()));
                }
            }

            if (json.ContainsKey("retractions"))
            {
                foreach ((string key, var value) in json["retractions"].Value<JObject>())
                {
                    var entries = SegregatedEntries.FromJson(value.Value<JObject>());
                    result.Retractions.Add(key, entries);
                }
            }

            return result;
        }

        public ResultForRoot(RootPosition position)
        {
            Position = position;
            ResultsBySelect = new Dictionary<Select, SelectResult>();
            Retractions = new Dictionary<string, SegregatedEntries>();
        }

        public ResultForRoot() :
            this(null)
        {
        }
    }

    public class SelectResult
    {
        public SegregatedEntries Root { get; set; }
        public Dictionary<string, SegregatedEntries> Children { get; set; }

        public static SelectResult FromJson(JObject json)
        {
            var result = new SelectResult();

            foreach ((string key, var value) in json)
            {
                var entries = SegregatedEntries.FromJson(value.Value<JObject>());
                if (key == "--")
                {
                    result.Root = entries;
                }
                else
                {
                    result.Children.Add(key, entries);
                }
            }

            return result;
        }

        public SelectResult()
        {
            Children = new Dictionary<string, SegregatedEntries>();
        }
    }
}
