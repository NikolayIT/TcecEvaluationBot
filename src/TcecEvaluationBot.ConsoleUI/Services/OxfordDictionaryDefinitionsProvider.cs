namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public class OxfordDictionaryDefinitionsProvider
    {
        private readonly HttpClient httpClient;

        public OxfordDictionaryDefinitionsProvider(string appId, string appKey)
        {
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("app_id", appId);
            this.httpClient.DefaultRequestHeaders.Add("app_key", appKey);
            this.httpClient.Timeout = new TimeSpan(0, 0, 0, 5);
        }

        public async Task<string> GetWordDefinition(string word)
        {
            try
            {
                var url =
                    $"https://od-api.oxforddictionaries.com:443/api/v2/entries/en-us/{word}?fields=definitions&strictMatch=false";
                var response = await this.httpClient.GetAsync(url);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                if (jsonResponse.Contains("No entry found matching supplied source_lang, word and provided filters"))
                {
                    return "word not found";
                }

                var data = JsonConvert.DeserializeObject<ResponseObject>(jsonResponse);
                return data.Results.FirstOrDefault()?.LexicalEntries.FirstOrDefault()?.Entries.FirstOrDefault()?.Senses.FirstOrDefault()?.Definitions.FirstOrDefault() ?? "Word not found.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public class ResponseObject
        {
            [JsonPropertyName("results")]
            public Result[] Results { get; set; }
        }

        public class Result
        {
            [JsonPropertyName("lexicalEntries")]
            public LexicalEntry[] LexicalEntries { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
        }

        public class LexicalEntry
        {
            [JsonPropertyName("entries")]
            public Entry[] Entries { get; set; }

            [JsonPropertyName("lexicalCategory")]
            public LexicalCategory LexicalCategory { get; set; }
        }

        public class LexicalCategory
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        public class Entry
        {
            [JsonPropertyName("senses")]
            public Sense[] Senses { get; set; }
        }

        public class Sense
        {
            [JsonPropertyName("definitions")]
            public string[] Definitions { get; set; }
        }
    }
}
