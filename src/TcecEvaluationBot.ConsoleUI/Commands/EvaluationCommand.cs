namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib;

    public class EvaluationCommand : ICommand
    {
        private readonly TwitchClient twitchClient;

        private readonly Options options;

        private readonly IList<Engine> engines;

        private readonly HttpClient httpClient;

        public EvaluationCommand(TwitchClient twitchClient, Options options, Settings settings)
        {
            this.twitchClient = twitchClient;
            this.options = options;

            this.engines = new List<Engine>();
            foreach (var engineSettings in settings.Engines)
            {
                this.engines.Add(new Engine(engineSettings, options));
            }

            this.httpClient = new HttpClient();
        }

        public string Execute(string message)
        {
            var engine = this.engines.FirstOrDefault()?.Name; // First registered engine will be default one
            var moveTime = this.options.DefaultEvaluationTime * 1000;
            var commandParts = message.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (commandParts.Length > 1)
            {
                for (var i = 1; i < commandParts.Length; i++)
                {
                    if (int.TryParse(commandParts[i], out var moveTimeArgument) && moveTimeArgument >= this.options.MinEvaluationTime
                                                                                && moveTimeArgument <= this.options.MaxEvaluationTime)
                    {
                        moveTime = moveTimeArgument * 1000;
                    }
                    else if (this.engines.Select(x => x.Name).Contains(commandParts[i].ToLower().Trim()))
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

        private string Evaluate(int moveTime, string engineName)
        {
            var livePgnAsString = this.GetTextContent("http://tcec.chessdom.com/live/live.pgn").GetAwaiter().GetResult();
            var fenPosition = this.ConvertPgnToFen(livePgnAsString);
            if (fenPosition == null)
            {
                Console.WriteLine("Invalid fen! See if file.pgn contains a valid PGN.");
                return null;
            }

            var engine =
                this.engines.FirstOrDefault(x => x.Name.ToLower().Trim() == engineName.ToLower().Trim())?.Evaluator
                ?? this.engines.FirstOrDefault()?.Evaluator;

            var evaluationMessage = engine?.GetEvaluation(fenPosition, moveTime);
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

        private class Engine : EngineSettings
        {
            public Engine(EngineSettings engineSettings, Options options)
            {
                this.Name = engineSettings.Name;
                this.Title = engineSettings.Title;
                this.Executable = engineSettings.Executable;
                this.PositionEvaluator = engineSettings.PositionEvaluator;

                var typeName = $"TcecEvaluationBot.ConsoleUI.Services.{this.PositionEvaluator}";
                var type = typeof(IPositionEvaluator).Assembly.GetType(typeName);
                this.Evaluator = (IPositionEvaluator)Activator.CreateInstance(type, options, this.Executable, this.Title);
            }

            public IPositionEvaluator Evaluator { get; }
        }
    }
}
