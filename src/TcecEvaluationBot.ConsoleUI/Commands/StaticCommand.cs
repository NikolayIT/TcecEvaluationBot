namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class StaticCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        public StaticCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);
        }

        public override string Execute(string message)
        {
            var fen = this.currentGameInfoProvider.GetInfo().Fen;
            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game?";
            }

            var info = GetStaticEvaluationLines(fen);
            if (info.Length == 0)
            {
                return "Unable to get static evaluation (probably the player is in check)";
            }

            var classicEvaluation = info[^5].Replace("Classical evaluation", string.Empty)
                .Replace("(white side)", string.Empty).Trim();
            var nnueEvaluation = info[^4].Replace("NNUE evaluation", string.Empty)
                .Replace("(white side)", string.Empty).Trim();
            var finalEvaluation = info[^3].Replace("Final evaluation", string.Empty)
                .Replace("[with scaled NNUE, hybrid, ...]", string.Empty)
                .Replace("(white side)", string.Empty).Trim();

            var result = new StringBuilder();
            result.Append($"({fen.GetMoveInfoFromFen()}) {finalEvaluation} • ");
            result.Append($"Classic: {classicEvaluation} • NNUE: {nnueEvaluation} • ");

            result.Append(this.GetPositionInfoFromLine(info[22]));
            for (var i = 8; i < 21; i++)
            {
                result.Append(" • " + this.GetPositionInfoFromLine(info[i]));
            }

            result.Append(" <Stockfish>");

            return result.ToString();
        }

        private static string[] GetStaticEvaluationLines(string fen)
        {
            for (var i = 0; i < 4; i++)
            {
                var process = new Process
                                  {
                                      StartInfo = new ProcessStartInfo
                                                      {
                                                          FileName = "stockfish.exe",
                                                          UseShellExecute = false,
                                                          RedirectStandardOutput = true,
                                                          RedirectStandardInput = true,
                                                          RedirectStandardError = true,
                                                          CreateNoWindow = true,
                                                      },
                                  };

                process.Start();

                process.StandardInput.WriteLine($"position fen {fen}");
                process.StandardInput.WriteLine("eval");
                process.StandardInput.Flush();
                Thread.Sleep(2000);
                if (!process.HasExited)
                {
                    process.Kill();
                }

                var info = process.StandardOutput.ReadToEnd().Split(Environment.NewLine);
                if (info.Length > 18)
                {
                    return info;
                }
            }

            return new string[0];
        }

        private string GetPositionInfoFromLine(string line)
        {
            var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return $"{lineParts[1]}({lineParts[^3]},{lineParts[^2]})";
        }
    }
}
