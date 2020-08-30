namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public class ResultForRoot
    {
        public ResultForRoot(RootPosition position)
        {
            this.Position = position;
            this.ResultsBySelect = new Dictionary<Select, SelectResult>();
            this.Retractions = new Dictionary<string, SegregatedEntries>();
        }

        public ResultForRoot()
            : this(null)
        {
        }

        public RootPosition Position { get; set; }

        public Dictionary<Select, SelectResult> ResultsBySelect { get; set; }

        public Dictionary<string, SegregatedEntries> Retractions { get; set; }

        public static ResultForRoot FromJson(JObject json)
        {
            var result = new ResultForRoot(
                RootPosition.FromJson(json["position"].Value<JObject>()));

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
    }
}
