namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Linq;

    public class QueryResponse
    {
        public QueryResponse()
        {
            this.Results = new List<ResultForRoot>();
        }

        public List<ResultForRoot> Results { get; set; }

        public static QueryResponse FromJson(JObject json)
        {
            var result = new QueryResponse();

            foreach (var entry in json["results"])
            {
                result.Results.Add(ResultForRoot.FromJson(entry.Value<JObject>()));
            }

            return result;
        }
    }
}
