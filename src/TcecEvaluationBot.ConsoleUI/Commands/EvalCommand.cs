namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TwitchLib;

    public class EvalCommand
    {
        private readonly TwitchClient twitchClient;

        private readonly Options options;

        private readonly IPositionEvaluator positionEvaluator;

        private readonly HttpClient httpClient;

        public EvalCommand(TwitchClient twitchClient, Options options)
        {
            this.twitchClient = twitchClient;
            this.options = options;
            this.positionEvaluator = new StockfishPositionEvaluator(options, "stockfish.exe");
            this.httpClient = new HttpClient();
        }

        public string Execute(string message)
        {
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
                    $"[{DateTime.UtcNow:HH:mm:ss}] Thinking {moveTime / 1000} sec., please wait.");
            }

            var evaluation = this.Evaluate(moveTime);
            return evaluation;
        }

        private string Evaluate(int moveTime)
        {
            var livePgnAsString = this.GetTextContent("http://tcec.chessdom.com/live/live.pgn").GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                Console.WriteLine("Invalid fen! See if file.pgn contains a valid PGN.");
                return null;
            }

            var evaluationMessage = this.positionEvaluator.GetEvaluation(fenPosition, (int)moveTime);
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

        private async Task<string> GetTextContent(string url)
        {
            var response = await this.httpClient.GetAsync($"{url}?noCache={Guid.NewGuid()}");
            var stringResult = await response.Content.ReadAsStringAsync();
            return stringResult;
        }
    }
}
