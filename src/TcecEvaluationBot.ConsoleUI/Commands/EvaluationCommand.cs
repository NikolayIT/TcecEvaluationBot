namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TcecEvaluationBot.ConsoleUI.Services;

    using TwitchLib;

    public class EvaluationCommand : ICommand
    {
        private readonly TwitchClient twitchClient;

        private readonly Options options;

        private readonly string[] availableEngines = { "komodo", "stockfish", "laser" };

        private readonly IPositionEvaluator stockfishPositionEvaluator;
        private readonly IPositionEvaluator komodoPositionEvaluator;
        private readonly IPositionEvaluator laserPositionEvaluator;

        private readonly HttpClient httpClient;

        public EvaluationCommand(TwitchClient twitchClient, Options options)
        {
            this.twitchClient = twitchClient;
            this.options = options;
            this.stockfishPositionEvaluator = new UciEnginePositionEvaluator(options, "stockfish.exe", "SF_9");
            this.komodoPositionEvaluator = new UciEnginePositionEvaluator(options, "komodo.exe", "Komodo_11.2.2, Courtesy of K authors");
            this.laserPositionEvaluator = new UciEnginePositionEvaluator(options, "laser.exe", "Laser_1.5");
            this.httpClient = new HttpClient();
        }

        public string Execute(string message)
        {
            var engine = "stockfish"; // TODO: Add default engine to options
            var moveTime = this.options.MoveTime;
            var commandParts = message.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (commandParts.Length > 1)
            {
                for (int i = 1; i < commandParts.Length; i++)
                {
                    if (int.TryParse(commandParts[i], out var moveTimeArgument) && moveTimeArgument >= 5
                                                                                && moveTimeArgument <= 30)
                    {
                        moveTime = moveTimeArgument * 1000;
                    }
                    else if (this.availableEngines.Contains(commandParts[i].ToLower().Trim()))
                    {
                        engine = commandParts[i].ToLower().Trim();
                    }
                }
            }

            if (this.options.ThinkingMessage)
            {
                this.twitchClient.SendMessage($"[{DateTime.UtcNow:HH:mm:ss}] Thinking {moveTime / 1000} sec., please wait.");
            }

            var evaluation = this.Evaluate(moveTime, engine);
            return evaluation;
        }

        private string Evaluate(int moveTime, string engine)
        {
            var livePgnAsString = this.GetTextContent("http://tcec.chessdom.com/live/live.pgn").GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                Console.WriteLine("Invalid fen! See if file.pgn contains a valid PGN.");
                return null;
            }

            string evaluationMessage;
            if (engine.ToLower().Trim() == "komodo")
            {
                evaluationMessage = this.komodoPositionEvaluator.GetEvaluation(fenPosition, moveTime);
            }
            else if (engine.ToLower().Trim() == "laser")
            {
                evaluationMessage = this.laserPositionEvaluator.GetEvaluation(fenPosition, moveTime);
            }
            else
            {
                evaluationMessage = this.stockfishPositionEvaluator.GetEvaluation(fenPosition, moveTime);
            }

            return evaluationMessage;
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

        private async Task<string> GetTextContent(string url)
        {
            var response = await this.httpClient.GetAsync($"{url}?noCache={Guid.NewGuid()}");
            var stringResult = await response.Content.ReadAsStringAsync();
            return stringResult;
        }
    }
}
