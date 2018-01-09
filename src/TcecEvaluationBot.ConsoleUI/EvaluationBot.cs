namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
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
                    if (arguments.ChatMessage.Message == "!eval"
                        || arguments.ChatMessage.Message.Trim().StartsWith("!eval "))
                    {
                        this.Log($"Received \"{arguments.ChatMessage.Message}\" from {arguments.ChatMessage.Username}");
                        this.EvalCommand(arguments.ChatMessage.Message);
                    }
                    else if (arguments.ChatMessage.Message == "!time"
                             || arguments.ChatMessage.Message.Trim().StartsWith("!time "))
                    {
                        this.Log($"Received \"{arguments.ChatMessage.Message}\" from {arguments.ChatMessage.Username}");
                        this.TimeCommand(arguments.ChatMessage.Message);
                    }
                };
            this.twitchClient.Connect();
        }

        private void EvalCommand(string message)
        {
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

        private void TimeCommand(string message)
        {
            var gamesInfoString =
                this.GetTextContent(
                        "http://tcec.chessdom.com/archive/TCEC%20Season%2011%20-%20Division%203%20Schedule.txt")
                    .GetAwaiter()
                    .GetResult();
            var stringReader = new StringReader(gamesInfoString);
            var header = stringReader.ReadLine();
            var startColumnIndex = header.IndexOf(" Start ", StringComparison.Ordinal) + 1;
            var durationColumnIndex = header.IndexOf(" Duration ", StringComparison.Ordinal) + 1;
            var ecoColumnIndex = header.IndexOf(" ECO ", StringComparison.Ordinal) + 1;
            string line = null;
            int countPlayed = 0;
            int count = 0;
            var totalTime = new TimeSpan();
            var lastStarted = (DateTime?)null;
            while ((line = stringReader.ReadLine()) != null)
            {
                count++;
                var time = line.Substring(durationColumnIndex, ecoColumnIndex - durationColumnIndex).Trim();
                if (!string.IsNullOrWhiteSpace(time))
                {
                    countPlayed++;
                    var timeParts = time.Split(':');
                    var timeSpan = new TimeSpan(0, int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));
                    totalTime += timeSpan;
                    //// Console.WriteLine(timeSpan);
                }
                else
                {
                    if (lastStarted == null)
                    {
                        // "12:46:53 on 2018.01.09"
                        var lastStartedAsString = line.Substring(startColumnIndex, durationColumnIndex - startColumnIndex).Trim();
                        lastStarted = DateTime.ParseExact(lastStartedAsString, "HH:mm:ss on yyyy.MM.dd", CultureInfo.InvariantCulture);
                    }
                }
            }

            if (countPlayed == 0)
            {
                this.twitchClient.SendMessage("No games played.");
                this.Log($"Responded with \"No games played.\"");
            }
            else
            {
                var averageGameTime = totalTime / countPlayed;
                var remainingTime = (count - countPlayed) * (averageGameTime + new TimeSpan(0, 0, 1, 0)); // +1 minute between games
                //// Console.WriteLine(lastStarted);
                //// Console.WriteLine(remainingTime);
                //// Console.WriteLine(lastStarted + remainingTime);
                //// Console.WriteLine($"\"{totalTime / countPlayed}\"");
                var response =
                    $"[{DateTime.Now.ToUniversalTime():HH:mm:ss}] {count - countPlayed} games left. Average duration: {(totalTime / countPlayed):hh\\:mm\\:ss}. Estimated division end: {lastStarted + remainingTime:R}.";
                this.twitchClient.SendMessage(response);
                this.Log($"Responded with \"{response}\"");
            }
        }

        private string Evaluate(int moveTime)
        {
            var livePgnAsString = this.GetTextContent("http://tcec.chessdom.com/live/live.pgn").GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                this.Log("Invalid fen! See if file.pgn contains a valid PGN.");
                return null;
            }

            var evaluationMessage = this.positionEvaluator.GetEvaluation(fenPosition, (int)moveTime);
            return evaluationMessage;
        }

        private async Task<string> GetTextContent(string url)
        {
            var response = await this.httpClient.GetAsync($"{url}?noCache={this.random.Next()}");
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
