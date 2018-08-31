namespace LichessApi
{
    using System;
    using System.Net.Http;
    using System.Threading;

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
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var response = this.httpClient.GetAsync(DbUrl + Uri.EscapeUriString(fen)).GetAwaiter().GetResult();
                    var stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var result = JsonConvert.DeserializeObject<DatabasePosition>(stringResponse);
                    return result;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in LichessApiClient.GetPositionInfo: {e.Message}");
                    Thread.Sleep(50);
                }
            }

            return null;
        }

        public TablebasePosition GetTablebaseInfo(string fen)
        {
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var response = this.httpClient.GetAsync(TbUrl + Uri.EscapeUriString(fen)).GetAwaiter().GetResult();
                    var stringResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var result = JsonConvert.DeserializeObject<TablebasePosition>(stringResponse);
                    return result;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in LichessApiClient.GetTablebaseInfo: {e.Message}");
                    Thread.Sleep(50);
                }
            }

            return null;
        }
    }
}
