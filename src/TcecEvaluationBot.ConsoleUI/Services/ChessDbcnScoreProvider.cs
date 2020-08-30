namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using TcecEvaluationBot.ConsoleUI.Services.Models;

    public class ChessDbcnScoreProvider
    {
        private static readonly string URL = "http://www.chessdb.cn/cdb.php";

        private static readonly MoveConversionService MoveConverter = new MoveConversionService();

        private readonly HttpClient client;

        public ChessDbcnScoreProvider()
        {
            this.client = new HttpClient
            {
                BaseAddress = new Uri(URL),
                Timeout = TimeSpan.FromMilliseconds(3000),
            };

            // Add an Accept header for JSON format.
            this.client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Dictionary<string, ChessDbcnScore> GetScores(string fen)
        {
            const string urlParameters = "?action=queryall&board={0}";

            try
            {
                HttpResponseMessage response = this.client.GetAsync(string.Format(urlParameters, fen)).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseStr = response.Content.ReadAsStringAsync().Result;
                    System.Diagnostics.Debug.WriteLine(responseStr);
                    if (responseStr.Contains("unknown"))
                    {
                        return new Dictionary<string, ChessDbcnScore> { };
                    }

                    return this.ParseScoresFromResponse(fen, responseStr);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        string.Format("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new Dictionary<string, ChessDbcnScore>();
        }

        private Dictionary<string, ChessDbcnScore> ParseScoresFromResponse(string fen, string responseStr)
        {
            Dictionary<string, ChessDbcnScore> scores = new Dictionary<string, ChessDbcnScore>();

            string[] byMoveStrs = responseStr.Split('|');
            foreach (var byMoveStr in byMoveStrs)
            {
                string[] parts = byMoveStr.Split(',');

                Dictionary<string, string> values = new Dictionary<string, string>();
                foreach (var part in parts)
                {
                    string[] kv = part.Split(':');
                    if (kv.Length < 2)
                    {
                        continue;
                    }

                    values.Add(kv[0], kv[1]);
                }

                values.TryGetValue("move", out string moveStr);
                values.TryGetValue("score", out string scoreStr);

                if (moveStr != null && scoreStr != null)
                {
                    try
                    {
                        // AlgebraicToSan produces '\0' bytes. TODO: fix?
                        var san = MoveConverter.AlgebraicToSan(fen, moveStr).Replace("\0", string.Empty);
                        scores.Add(san, new ChessDbcnScore(scoreStr));
                    }
                    catch
                    {
                    }
                }
            }

            return scores;
        }
    }
}
