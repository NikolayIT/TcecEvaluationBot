namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using TcecEvaluationBot.Pgn;

    public class CurrentGameInfoProvider
    {
        private readonly string livePgnUrl;

        private readonly HttpClient httpClient;

        private readonly PgnParser pgnParser;

        public CurrentGameInfoProvider(string livePgnUrl)
        {
            this.livePgnUrl = livePgnUrl;
            this.pgnParser = new PgnParser();
            this.httpClient = new HttpClient();
        }

        public GameInfo GetInfo()
        {
            var livePgnAsString = this.GetTextContent(this.livePgnUrl).GetAwaiter().GetResult();
            if (livePgnAsString.Trim().Contains("[Result \"*\"]"))
            {
                livePgnAsString = livePgnAsString.Replace(
                    @"[Result ""*""]  
* ",
                    string.Empty);
            }

            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                Console.WriteLine("Invalid fen! See if file.pgn contains a valid PGN.");
            }

            var lastMove = this.ExtractLastMove(livePgnAsString);

            return new GameInfo { Fen = fenPosition, LastMove = lastMove, };
        }

        private string ExtractLastMove(string pgn)
        {
            try
            {
                var lastMove = this.pgnParser.ParseFromString(pgn).Games.Last().Moves.Last();
                return $"{lastMove.Number}{(lastMove.Color == Color.White ? 'w' : 'b')}. {lastMove.San}";
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception during parsing last move: {e.Message}");
                return string.Empty;
            }
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
            for (var i = 0; i < 15; i++)
            {
                try
                {
                    var response = await this.httpClient.GetAsync($"{url}?noCache={Guid.NewGuid()}");
                    var stringResult = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(stringResult))
                    {
                        return stringResult;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                Thread.Sleep(100);
            }

            return string.Empty;
        }

        public class GameInfo
        {
            public string Fen { get; set; }

            public string LastMove { get; set; }
        }
    }
}
