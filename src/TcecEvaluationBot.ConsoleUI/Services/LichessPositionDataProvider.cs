namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Net.Http;

    using Newtonsoft.Json;

    using TcecEvaluationBot.ConsoleUI.Services.LichessModels;

    public class LichessPositionDataProvider
    {
        private readonly HttpClient httpClient;

        public LichessPositionDataProvider()
        {
            this.httpClient = new HttpClient();
        }

        public LichessPosition GetPositionInfo(string fen)
        {
            var response = this.httpClient.GetAsync($"https://explorer.lichess.ovh/master?fen=" + Uri.EscapeUriString(fen)).GetAwaiter().GetResult();
            var stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = JsonConvert.DeserializeObject<LichessPosition>(stringResponse);
            return result;
        }
    }
}
