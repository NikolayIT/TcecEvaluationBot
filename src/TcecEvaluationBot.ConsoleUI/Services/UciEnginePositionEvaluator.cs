namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    public class UciEnginePositionEvaluator : IPositionEvaluator
    {
        private readonly Options options;

        private readonly string executableFileName;

        private readonly string engineSignature;

        public UciEnginePositionEvaluator(Options options, string executableFileName, string engineSignature)
        {
            this.options = options;
            this.executableFileName = executableFileName;
            this.engineSignature = engineSignature;
        }

        public string GetEvaluation(string fenPosition, int moveTime)
        {
            if (!File.Exists(this.executableFileName))
            {
                return $"File \"{this.executableFileName}\" not found!";
            }

            var process = new Process
                              {
                                  StartInfo = new ProcessStartInfo
                                                  {
                                                      FileName = this.executableFileName,
                                                      UseShellExecute = false,
                                                      RedirectStandardOutput = true,
                                                      RedirectStandardInput = true,
                                                      RedirectStandardError = true,
                                                      CreateNoWindow = true,
                                                  }
                              };

            process.Start();

            process.StandardInput.WriteLine($"setoption name Threads value {this.options.Threads}");
            process.StandardInput.WriteLine($"setoption name Hash value {this.options.HashSize}");
            if (!string.IsNullOrWhiteSpace(this.options.SyzygyPath))
            {
                process.StandardInput.WriteLine($"setoption name SyzygyPath value {this.options.SyzygyPath}");
            }

            process.StandardInput.WriteLine($"position fen {fenPosition}");
            process.StandardInput.WriteLine($"go movetime {moveTime}");
            process.StandardInput.WriteLine($"setoption name Contempt value 0");
            process.StandardInput.Flush();

            string currentLine = null;
            try
            {
                string lastStatsLine = null;
                while (!process.StandardOutput.EndOfStream)
                {
                    currentLine = process.StandardOutput.ReadLine();
                    if (currentLine == null)
                    {
                        continue;
                    }

                    // Console.WriteLine(currentLine);
                    if (currentLine.StartsWith("bestmove") && lastStatsLine != null)
                    {
                        Console.WriteLine(lastStatsLine);
                        var fenParts = fenPosition.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var moveNumber = fenParts[fenParts.Length - 1].Trim();
                        var currentPlayer = fenPosition.Contains(" b ") ? 'b' : 'w';
                        var depth = lastStatsLine.Split(" depth ")[1].Split(" ")[0];
                        var tableBaseHits = lastStatsLine.Split(" tbhits ")[1].Split(" ")[0];
                        var cp = GetCp(fenPosition, lastStatsLine);
                        var best = currentLine.Split("bestmove ")[1].Split(" ")[0];
                        var ponder = currentLine.Contains("ponder ") ? currentLine.Split("ponder ")[1] : string.Empty;
                        var outputMessage = $"{cp} d{depth} (tb {tableBaseHits}) pv {best} {ponder} ({moveNumber}{currentPlayer}) <{this.engineSignature}>";
                        return outputMessage;
                    }

                    // Komodo: info depth 99 time 33 nodes 197546 score mate -1 nps 5970267 hashfull 0 tbhits 0 pv a1a2 a7h7
                    if (currentLine.Contains(" depth ") && currentLine.Contains(" tbhits ")
                                                        && (currentLine.Contains(" cp ") || currentLine.Contains(" mate ")))
                    {
                        lastStatsLine = currentLine;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                return $"Error has occurred: {e.Message}";
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }

                process.Dispose();
            }

            Thread.Sleep(2000);
            Console.WriteLine($"Last line: \"{currentLine}\"");
            return $"[{DateTime.UtcNow:HH:mm:ss}] No active game? Please try again.";
        }

        private static string GetCp(string fenPosition, string lastStatsLine)
        {
            if (int.TryParse(lastStatsLine.Split(" cp ")[1].Split(" ")[0], out int cp))
            {
                if (fenPosition.Contains(" b "))
                {
                    cp = -cp;
                }

                return $"{cp / 100.0M:0.00}";
            }

            if (int.TryParse(lastStatsLine.Split(" mate ")[1].Split(" ")[0], out int mate))
            {
                if (fenPosition.Contains(" b "))
                {
                    mate = -mate;
                }

                return $"M{mate}";
            }

            return "?.??";
        }
    }
}
