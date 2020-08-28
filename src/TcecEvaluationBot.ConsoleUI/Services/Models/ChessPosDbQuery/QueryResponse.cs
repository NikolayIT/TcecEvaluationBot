using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public class QueryResponse
    {
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

        public QueryResponse()
        {
            Results = new List<ResultForRoot>();
        }
    }
}
