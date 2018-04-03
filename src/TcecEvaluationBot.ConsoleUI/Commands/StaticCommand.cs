namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    using TcecEvaluationBot.ConsoleUI.Services;

    public class StaticCommand : BaseCommand
    {
        private readonly CurrentGameInfoProvider currentGameInfoProvider;

        public StaticCommand()
        {
            this.currentGameInfoProvider = new CurrentGameInfoProvider();
        }

        public override string Execute(string message)
        {
            var fen = this.currentGameInfoProvider.GetFen();
            if (string.IsNullOrWhiteSpace(fen))
            {
                return $"[{DateTime.UtcNow:HH:mm:ss}] No active game?";
            }

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
            Thread.Sleep(200);
            if (!process.HasExited)
            {
                process.Kill();
            }

            var result = new StringBuilder();
            result.Append($"[{DateTime.UtcNow:HH:mm:ss}] ({fen.GetMoveInfoFromFen()}) ");

            var info = process.StandardOutput.ReadToEnd().Split(Environment.NewLine);
            result.Append(this.GetPositionInfoFromLine(info[18]));
            for (var i = 4; i < 17; i++)
            {
                result.Append(this.GetPositionInfoFromLine(info[i]));
            }

            result.Append("<Stockfish>");

            return result.ToString();
        }

        private string GetPositionInfoFromLine(string line)
        {
            var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return $"{lineParts[0]}({lineParts[lineParts.Length - 2]},{lineParts[lineParts.Length - 1]}) ";
        }
    }
}
