namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using AngleSharp;
    using AngleSharp.Html.Parser;

    using Newtonsoft.Json;

    public class ChessProgrammingPageContentProvider
    {
        private const string BaseUrl = "https://www.chessprogramming.org/api.php?action=parse&prop=text&formatversion=2&format=json&redirects&page=";

        private readonly HttpClient httpClient;

        public ChessProgrammingPageContentProvider()
        {
            this.httpClient = new HttpClient { Timeout = new TimeSpan(0, 0, 0, 5) };
        }

        public async Task<string> GetContent(string word)
        {
            try
            {
                var url = BaseUrl + word;
                var response = await this.httpClient.GetAsync(url);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                if (jsonResponse.Contains("The page you specified doesn't exist."))
                {
                    return null;
                }

                var data = JsonConvert.DeserializeObject<ResponseObject>(jsonResponse);
                if (string.IsNullOrWhiteSpace(data.Parse?.Text))
                {
                    return null;
                }

                var parser = new HtmlParser();
                var document = parser.ParseDocument(data.Parse.Text);

                foreach (var element in document.QuerySelectorAll(".thumb").ToList())
                {
                    element.Remove();
                }

                foreach (var element in document.QuerySelectorAll("#toc").ToList())
                {
                    element.Remove();
                }

                foreach (var element in document.QuerySelectorAll("#contentSub").ToList())
                {
                    element.Remove();
                }

                foreach (var element in document.QuerySelectorAll("h1").ToList())
                {
                    element.Remove();
                }

                foreach (var element in document.QuerySelectorAll("p").Where(x => x.InnerHtml.Contains("title=\"Main Page\">Home</a>")).ToList())
                {
                    element.Remove();
                }

                var text = document.QuerySelector("p").TextContent;
                text = Regex.Replace(text, @"\[[0-9]*\]", string.Empty);
                text = text.Trim();

                return MaxLength(text, 350);
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
            [JsonPropertyName("parse")]
            public Parse Parse { get; set; }
        }

        public class Parse
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }
    }
}
