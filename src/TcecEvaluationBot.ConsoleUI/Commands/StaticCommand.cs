namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    public class StaticCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        public StaticCommand(Settings settings)
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider(settings.LivePgnUrl);
        }

        public override string Execute(string message)
        {
            var fen = this.currentGameInfoProvider.GetFen();
            if (string.IsNullOrWhiteSpace(fen))
            {
                return "No active game?";
            }

            var info = GetStaticEvaluationLines(fen);
            if (info.Length == 0)
            {
                return "Unable to get static evaluation";
            }

            var result = new StringBuilder();
            result.Append($"({fen.GetMoveInfoFromFen()}) ");

            result.Append(this.GetPositionInfoFromLine(info[18]));
            for (var i = 4; i < 17; i++)
            {
                result.Append(" • " + this.GetPositionInfoFromLine(info[i]));
            }

            result.Append("<Stockfish>");

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
                Thread.Sleep(300);
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
            return $"{lineParts[0]}({lineParts[lineParts.Length - 2]},{lineParts[lineParts.Length - 1]}) ";
        }
    }
}
