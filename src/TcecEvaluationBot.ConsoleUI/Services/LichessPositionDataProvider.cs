namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Net.Http;

    using Newtonsoft.Json;

    using TcecEvaluationBot.ConsoleUI.Services.LichessModels;

    public class LichessPositionDataProvider
    {
        private readonly string lichessDbUrl;

        private readonly HttpClient httpClient;

        public LichessPositionDataProvider(string lichessDbUrl)
        {
            this.lichessDbUrl = lichessDbUrl;
            this.httpClient = new HttpClient();
        }

        public LichessPosition GetPositionInfo(string fen)
        {
            var response = this.httpClient.GetAsync(this.lichessDbUrl + Uri.EscapeUriString(fen)).GetAwaiter().GetResult();
            var stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = JsonConvert.DeserializeObject<LichessPosition>(stringResponse);
            return result;
        }
    }
}
