namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TwitchLib;
    using TwitchLib.Models.Client;

    public class EvaluationBot
    {
        private readonly Options options;

        private readonly Random random;

        private readonly HttpClient httpClient;

        private readonly TwitchClient twitchClient;

        private DateTime lastMessage = DateTime.Now.AddDays(-1);

        public EvaluationBot(Options options)
        {
            this.options = options;
            this.random = new Random();
            this.httpClient = new HttpClient();
            var credentials = new ConnectionCredentials(options.TwitchUserName, options.TwitchAccessToken);
            this.twitchClient = new TwitchClient(credentials, options.TwitchChannelName);
        }

        public void Run()
        {
            //// Console.WriteLine(this.Evaluate());

            this.twitchClient.OnConnected += (sender, arguments) => this.Log("Connected!");
            this.twitchClient.OnJoinedChannel += (sender, arguments) => this.Log($"Joined to {arguments.Channel}!");
            this.twitchClient.OnMessageReceived += (sender, arguments) =>
                {
                    if (arguments.ChatMessage.Message == "!eval" || arguments.ChatMessage.Message.Trim().StartsWith("!eval "))
                    {
                        this.Log($"Received \"{arguments.ChatMessage.Message}\" from {arguments.ChatMessage.Username}");
                        if ((DateTime.Now - this.lastMessage).TotalSeconds >= this.options.CooldownTime)
                        {
                            this.lastMessage = DateTime.Now;
                            this.twitchClient.SendMessage($"[{DateTime.Now.ToUniversalTime():HH:mm:ss}] Thinking {this.options.MoveTime / 1000} seconds, please wait.");
                            var evaluation = this.Evaluate();
                            this.twitchClient.SendMessage(evaluation);
                            //// TODO: Emojis for eval 0.00  athUG 0.25  athSM 0.50  athS 1.00  athO 2.00  athC
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
                this.Log("Invalid fen! See if file.pgn contains a valid PGN.");
                return null;
            }

            var evaluationMessage = this.GetStockfishEvaluation(fenPosition);
            return $"{evaluationMessage} <SF040118>";
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
            sfProcess.StandardInput.WriteLine($"setoption name Threads value {this.options.Threads}");
            sfProcess.StandardInput.WriteLine($"setoption name Hash value {this.options.HashSize}");
            if (!string.IsNullOrWhiteSpace(this.options.SyzygyPath))
            {
                sfProcess.StandardInput.WriteLine($"setoption name SyzygyPath value {this.options.SyzygyPath}");
            }

            sfProcess.StandardInput.WriteLine($"go movetime {this.options.MoveTime}");

            try
            {
                string line = null;
                while (!sfProcess.StandardOutput.EndOfStream)
                {
                    var currentLine = sfProcess.StandardOutput.ReadLine();
                    //// Console.WriteLine(currentLine);
                    if (currentLine?.StartsWith("bestmove") == true)
                    {
                        Console.WriteLine(line);
                        var depth = line.Split(" depth ")[1].Split(" ")[0];
                        var tbhits = line.Split(" tbhits ")[1].Split(" ")[0];
                        var cp = int.Parse(line.Split(" cp ")[1].Split(" ")[0]);
                        char currentPlayer = 'w';
                        if (fenPosition.Contains(" b "))
                        {
                            cp = -cp;
                            currentPlayer = 'b';
                        }

                        var best = currentLine.Split("bestmove ")[1].Split(" ")[0];
                        var ponder = currentLine.Contains("ponder ") ? currentLine.Split("ponder ")[1] : string.Empty;
                        //// var pv = line.Split(" pv ")[1].Split(" ");
                        return $"{cp / 100.0M:0.00} d{depth} (tb {tbhits}) pv {best} {ponder} ({currentPlayer})";
                    }

                    line = currentLine;
                }
            }
            catch (Exception e)
            {
                this.Log("Error: " + e);
                return $"Error has occurred: {e.Message}";
            }
            finally
            {
                sfProcess.Dispose();
            }

            return "No active game? Please try again.";
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
