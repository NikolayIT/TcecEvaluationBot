namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class EvaluationCommand : BaseCommand
    {
        private readonly TwitchClient twitchClient;

        private readonly Options options;

        private readonly IDictionary<string, IPositionEvaluator> engines;

        private readonly string defaultEngine;

        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        public EvaluationCommand(TwitchClient twitchClient, Options options, Settings settings)
        {
            this.twitchClient = twitchClient;
            this.options = options;

            if (settings.Engines.Length == 0)
            {
                throw new Exception("No engines are registered.");
            }

            this.defaultEngine = settings.Engines.FirstOrDefault()?.Names?.FirstOrDefault()?.ToLower().Trim();
            this.engines = new Dictionary<string, IPositionEvaluator>();
            foreach (var engineSettings in settings.Engines)
            {
                var typeName = $"TcecEvaluationBot.ConsoleUI.Services.{engineSettings.PositionEvaluator}";
                var type = typeof(IPositionEvaluator).Assembly.GetType(typeName);
                if (Activator.CreateInstance(type, options, engineSettings.Executable, engineSettings.Title, engineSettings.Arguments) is IPositionEvaluator evaluator)
                {
                    foreach (var engineName in engineSettings.Names)
                    {
                        this.engines.Add(engineName.ToLower().Trim(), evaluator);
                    }
                }
            }

            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);
        }

        public override string Execute(string message)
        {
            var engine = this.defaultEngine;
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
                    else if (this.engines.Keys.Contains(commandParts[i].ToLower().Trim()))
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
            var fenPosition = this.currentGameInfoProvider.GetFen();
            if (fenPosition == null)
            {
                return "fenPosition is null. No active game?";
            }

            var engine = this.engines[engineName];
            var evaluationMessage = engine?.GetEvaluation(fenPosition, moveTime);
            return evaluationMessage;
        }
    }
}
