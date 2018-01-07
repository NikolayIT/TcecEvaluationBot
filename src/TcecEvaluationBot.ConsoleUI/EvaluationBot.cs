namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using TwitchLib;
    using TwitchLib.Models.Client;

    public class EvaluationBot
    {
        private readonly Random random;

        private readonly HttpClient httpClient;

        private readonly TwitchClient twitchClient;

        public EvaluationBot(string twitchUserName, string twitchAccessToken)
        {
            this.random = new Random();
            this.httpClient = new HttpClient();
            var credentials = new ConnectionCredentials(twitchUserName, twitchAccessToken);
            this.twitchClient = new TwitchClient(credentials, "tcecpoc");
        }

        public void Run()
        {
            this.twitchClient.OnConnected += (sender, arguments) => Console.WriteLine("Connected!");
            this.twitchClient.OnJoinedChannel += (sender, arguments) => Console.WriteLine($"Joined to {arguments.Channel}!");
            this.twitchClient.OnMessageReceived += (sender, arguments) =>
                {
                    if (arguments.ChatMessage.Message.Trim().StartsWith("!eval"))
                    {
                        var evaluation = this.Evaluate();
                        Console.WriteLine($"!!!{arguments.ChatMessage.Message}");
                        this.twitchClient.SendMessage(evaluation);
                    }
                };
            this.twitchClient.Connect();
        }

        private string Evaluate()
        {
            var sw = Stopwatch.StartNew();
            var livePgnAsString = this.GetLivePgn().GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                Console.WriteLine("Invalid fen! See if file.pgn contains valid PGN.");
                return null;
            }

            Console.WriteLine(sw.Elapsed);

            var evaluationMessage = this.GetStockfishEvaluation(fenPosition);
            return evaluationMessage;
        }

        private string GetStockfishEvaluation(string fenPosition)
        {
            var sfProcess = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                                    {
                                                        FileName = "stockfish.exe",
                                                        UseShellExecute = false,
                                                        RedirectStandardOutput = true,
                                                        RedirectStandardInput = true,
                                                        CreateNoWindow = true
                                                    }
                                };
            sfProcess.Start();

            sfProcess.StandardInput.WriteLine($"position fen \"{fenPosition}\"");
            sfProcess.StandardInput.WriteLine($"go movetime 1000");

            string line = null;
            while (!sfProcess.StandardOutput.EndOfStream)
            {
                var currentLine = sfProcess.StandardOutput.ReadLine();
                Console.WriteLine(currentLine);
                if (currentLine?.StartsWith("bestmove") == true)
                {
                    return line + " -- " + currentLine;
                }

                line = currentLine;
            }

            return line;
        }

        private async Task<string> GetLivePgn()
        {
            var response = await this.httpClient.GetAsync("http://tcec.chessdom.com/live/live.pgn?noCache=" + this.random.Next());
            var stringResult = await response.Content.ReadAsStringAsync();
            return stringResult;
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
                                                      CreateNoWindow = true
                                                  }
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
                Console.WriteLine(fenPosition);
            }

            return fenPosition;
        }
    }
}
