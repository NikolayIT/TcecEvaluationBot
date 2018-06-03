namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class CurrentGameInfoProvider
    {
        private readonly string livePgnUrl;

        private readonly HttpClient httpClient;

        public CurrentGameInfoProvider(string livePgnUrl)
        {
            this.livePgnUrl = livePgnUrl;
            this.httpClient = new HttpClient();
        }

        public string GetFen()
        {
            var livePgnAsString = this.GetTextContent(this.livePgnUrl).GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                Console.WriteLine("Invalid fen! See if file.pgn contains a valid PGN.");
            }

            return fenPosition;
        }

        private string ConvertPgnToFen(string livePgnAsString)
        {
            File.WriteAllText("file.pgn", livePgnAsString);
            var process = new Process
                              {
                                  StartInfo = new ProcessStartInfo
                                                  {
                                                      FileName = "pgn-extract.exe",
                                                      Arguments = "-F file.pgn",
                                                      UseShellExecute = false,
                                                      RedirectStandardOutput = true,
                                                      CreateNoWindow = true,
                                                  },
                              };
            process.Start();

            var lastMeaningfulLine = string.Empty;
            while (!process.StandardOutput.EndOfStream)
            {
                var currentLine = process.StandardOutput.ReadLine();
                if (currentLine != string.Empty && currentLine != "*")
                {
                    lastMeaningfulLine = currentLine;
                    //// Console.WriteLine(lastMeaningfulLine);
                }
            }

            var outputParts = lastMeaningfulLine?.Split("\"");
            string fenPosition = null;
            if (outputParts?.Length > 2)
            {
                fenPosition = outputParts[1];
                //// Console.WriteLine(fenPosition);
            }

            return fenPosition;
        }

        private async Task<string> GetTextContent(string url)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var response = await this.httpClient.GetAsync($"{url}?noCache={Guid.NewGuid()}");
                    var stringResult = await response.Content.ReadAsStringAsync();
                    return stringResult;
                }
                catch (Exception)
                {
                    Thread.Sleep(500);
                }
            }

            return string.Empty;
        }
    }
}
