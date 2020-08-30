namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public class SelectResult
    {
        public SelectResult()
        {
            this.Children = new Dictionary<string, SegregatedEntries>();
        }

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
    }
}
