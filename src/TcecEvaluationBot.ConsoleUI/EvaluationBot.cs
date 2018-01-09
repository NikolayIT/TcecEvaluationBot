namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using TwitchLib;
    using TwitchLib.Events.Client;
    using TwitchLib.Models.Client;

    public class EvaluationBot
    {
        private readonly Options options;

        private readonly Random random;

        private readonly HttpClient httpClient;

        private readonly TwitchClient twitchClient;

        private readonly IPositionEvaluator positionEvaluator;

        private DateTime lastMessage = DateTime.Now.AddDays(-1);

        public EvaluationBot(Options options)
        {
            this.options = options;
            this.random = new Random();
            this.httpClient = new HttpClient();
            var credentials = new ConnectionCredentials(options.TwitchUserName, options.TwitchAccessToken);
            this.twitchClient = new TwitchClient(credentials, options.TwitchChannelName);
            this.positionEvaluator = new StockfishPositionEvaluator(options, "stockfish.exe");
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
                        this.EvalCommand(arguments.ChatMessage.Message, arguments.ChatMessage.Username);
                    }
                };
            this.twitchClient.Connect();
        }

        private void EvalCommand(string message, string userName)
        {
            this.Log($"Received \"{message}\" from {userName}");
            if ((DateTime.Now - this.lastMessage).TotalSeconds >= this.options.CooldownTime)
            {
                this.lastMessage = DateTime.Now;

                var moveTime = this.options.MoveTime;
                var commandParts = message.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (commandParts.Length > 1 && int.TryParse(commandParts[1], out var moveTimeArgument) && moveTimeArgument >= 5
                    && moveTimeArgument <= 30)
                {
                    moveTime = moveTimeArgument * 1000;
                }

                if (this.options.ThinkingMessage)
                {
                    this.twitchClient.SendMessage(
                        $"[{DateTime.Now.ToUniversalTime():HH:mm:ss}] Thinking {moveTime / 1000} sec., please wait.");
                }

                var evaluation = this.Evaluate(moveTime);
                this.twitchClient.SendMessage(evaluation);
                this.Log($"Responded with \"{evaluation}\"");
            }
            else
            {
                var cooldownRemaining = this.options.CooldownTime - (DateTime.Now - this.lastMessage).TotalSeconds;
                this.twitchClient.SendMessage(
                    $"[{DateTime.Now.ToUniversalTime():HH:mm:ss}] You evaluate! ({cooldownRemaining:0.0})");
                this.Log($"Cooldown: {cooldownRemaining:0.0} seconds remaining.");
            }
        }

        private string Evaluate(int moveTime)
        {
            var livePgnAsString = this.GetLivePgn().GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                this.Log("Invalid fen! See if file.pgn contains a valid PGN.");
                return null;
            }

            var evaluationMessage = this.positionEvaluator.GetEvaluation(fenPosition, (int)moveTime);
            return evaluationMessage;
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
