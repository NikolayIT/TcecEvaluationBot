namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TwitchLib;
    using TwitchLib.Models.Client;

    public class EvaluationBot
    {
        private readonly Random random;

        private readonly HttpClient httpClient;

        private readonly TwitchClient twitchClient;

        private DateTime lastMessage = DateTime.Now.AddDays(-1);

        public EvaluationBot(string twitchUserName, string twitchAccessToken)
        {
            this.random = new Random();
            this.httpClient = new HttpClient();
            var credentials = new ConnectionCredentials(twitchUserName, twitchAccessToken);
            this.twitchClient = new TwitchClient(credentials, "tcecpoc");
        }

        public void Run()
        {
            // Console.WriteLine(this.Evaluate());

            this.twitchClient.OnConnected += (sender, arguments) => this.Log("Connected!");
            this.twitchClient.OnJoinedChannel += (sender, arguments) => this.Log($"Joined to {arguments.Channel}!");
            this.twitchClient.OnMessageReceived += (sender, arguments) =>
                {
                    if (arguments.ChatMessage.Message.Trim().StartsWith("!eval"))
                    {
                        this.Log($"Received {arguments.ChatMessage.Message} from {arguments.ChatMessage.Username}");
                        if ((DateTime.Now - this.lastMessage).TotalSeconds >= 30)
                        {
                            this.lastMessage = DateTime.Now;
                            this.twitchClient.SendMessage($"[{DateTime.Now.ToUniversalTime():HH:mm:ss}] Thinking 10 seconds, please wait.");
                            var evaluation = this.Evaluate();
                            this.twitchClient.SendMessage($"{evaluation} <SF040118>");
                            // TODO: Emojis for eval 0.00  athUG 0.25  athSM 0.50  athS 1.00  athO 2.00  athC
                            this.Log($"Responded with {evaluation}");
                        }
                        else
                        {
                            this.Log($"Cooldown: {(this.lastMessage - DateTime.Now).TotalSeconds} seconds.");
                        }
                    }
                };
            this.twitchClient.Connect();
        }

        private string Evaluate()
        {
            var livePgnAsString = this.GetLivePgn().GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                this.Log("Invalid fen! See if file.pgn contains valid PGN.");
                return null;
            }

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
            sfProcess.StandardInput.WriteLine($"go movetime 10000");

            string line = null;
            while (!sfProcess.StandardOutput.EndOfStream)
            {
                var currentLine = sfProcess.StandardOutput.ReadLine();
                //// Console.WriteLine(currentLine);
                if (currentLine?.StartsWith("bestmove") == true)
                {
                    Console.WriteLine(line);
                    var depth = line.Split(" depth ")[1].Split(" ")[0];
                    var cp = int.Parse(line.Split(" cp ")[1].Split(" ")[0]);

                    var best = currentLine.Split("bestmove ")[1].Split(" ")[0];
                    var ponder = currentLine.Split("ponder ")[1];
                    //// var pv = line.Split(" pv ")[1].Split(" ");
                    return $"{(cp / 100):0.00} d{depth} pv {best} {ponder}";
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
                //// Console.WriteLine(fenPosition);
            }

            return fenPosition;
        }

        private void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now}]");
            Console.ResetColor();
            Console.WriteLine($" {message}");
        }
    }
}
