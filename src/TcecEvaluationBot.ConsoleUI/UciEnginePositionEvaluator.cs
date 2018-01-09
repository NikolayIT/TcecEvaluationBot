namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    public class UciEnginePositionEvaluator : IPositionEvaluator
    {
        private readonly Options options;

        private readonly string еxecutableFileName;

        private readonly string engineSignature;

        public UciEnginePositionEvaluator(Options options, string еxecutableFileName, string engineSignature)
        {
            this.options = options;
            this.еxecutableFileName = еxecutableFileName;
            this.engineSignature = engineSignature;
        }

        public string GetEvaluation(string fenPosition, int moveTime)
        {
            var sfProcess = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                                    {
                                                        FileName = this.еxecutableFileName,
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

            sfProcess.StandardInput.WriteLine($"go movetime {moveTime}");

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
                        return $"{cp / 100.0M:0.00} d{depth} (tb {tbhits}) pv {best} {ponder} ({currentPlayer}) <{this.engineSignature}>";
                    }

                    line = currentLine;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                return $"Error has occurred: {e.Message}";
            }
            finally
            {
                sfProcess.Dispose();
            }

            Thread.Sleep(2000);
            return $"[{DateTime.UtcNow:HH:mm:ss}] No active game? Please try again.";
        }
    }
}
