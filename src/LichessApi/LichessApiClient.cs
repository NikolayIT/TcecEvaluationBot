namespace LichessApi
{
    using System;
    using System.Net.Http;

    using LichessApi.Models;

    using Newtonsoft.Json;

    public class LichessApiClient : ILichessApiClient
    {
        private const string DbUrl = "https://explorer.lichess.ovh/master?fen=";

        private const string TbUrl = "https://tablebase.lichess.ovh/standard?fen=";

        private readonly HttpClient httpClient;

        public LichessApiClient()
        {
            this.httpClient = new HttpClient();
        }

        public DatabasePosition GetPositionInfo(string fen)
        {
            var response = this.httpClient.GetAsync(DbUrl + Uri.EscapeUriString(fen)).GetAwaiter().GetResult();
            var stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = JsonConvert.DeserializeObject<DatabasePosition>(stringResponse);
            return result;
        }

        public TablebasePosition GetTablebaseInfo(string fen)
        {
            var response = this.httpClient.GetAsync(TbUrl + Uri.EscapeUriString(fen)).GetAwaiter().GetResult();
            var stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var result = JsonConvert.DeserializeObject<TablebasePosition>(stringResponse);
            return result;
        }
    }
}
