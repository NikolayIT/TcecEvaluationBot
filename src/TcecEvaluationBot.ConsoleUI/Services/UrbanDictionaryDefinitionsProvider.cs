namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public class UrbanDictionaryDefinitionsProvider
    {
        private const string BaseUrl = "http://api.urbandictionary.com/v0/define?term=";

        private readonly HttpClient httpClient;

        public UrbanDictionaryDefinitionsProvider()
        {
            this.httpClient = new HttpClient { Timeout = new TimeSpan(0, 0, 0, 5) };
        }

        public async Task<string> GetWordDefinition(string word)
        {
            try
            {
                var url = BaseUrl + word;
                var response = await this.httpClient.GetAsync(url);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<ResponseObject>(jsonResponse);
                if (data.List.Length == 0)
                {
                    return "word not found";
                }

                return MaxLength(
                    data.List.FirstOrDefault()?.Definition?.Replace("[", string.Empty).Replace("]", string.Empty)
                    ?? "word not found",
                    250);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string MaxLength(string value, int maxLength)
        {
            if (value.Length > maxLength)
            {
                return value.Substring(0, maxLength - 3) + "...";
            }

            return value;
        }

        public class ResponseObject
        {
            [JsonPropertyName("list")]
            public List[] List { get; set; }
        }

        public class List
        {
            [JsonPropertyName("definition")]
            public string Definition { get; set; }
        }
    }
}
