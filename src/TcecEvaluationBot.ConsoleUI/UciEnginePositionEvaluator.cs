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
            var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                                    {
                                                        FileName = this.еxecutableFileName,
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
            process.StandardInput.Flush();

            try
            {
                string lastStatsLine = null;
                while (!process.StandardOutput.EndOfStream)
                {
                    var currentLine = process.StandardOutput.ReadLine();
                    //// Console.WriteLine(currentLine);
                    if (currentLine?.StartsWith("bestmove") == true && lastStatsLine != null)
                    {
                        Console.WriteLine(lastStatsLine);
                        var currentPlayer = fenPosition.Contains(" b ") ? 'b' : 'w';
                        var depth = lastStatsLine.Split(" depth ")[1].Split(" ")[0];
                        var tbhits = lastStatsLine.Split(" tbhits ")[1].Split(" ")[0];
                        var cp = GetCp(fenPosition, lastStatsLine);
                        var best = currentLine.Split("bestmove ")[1].Split(" ")[0];
                        var ponder = currentLine.Contains("ponder ") ? currentLine.Split("ponder ")[1] : string.Empty;
                        var outputMessage = $"{cp} d{depth} (tb {tbhits}) pv {best} {ponder} ({currentPlayer}) <{this.engineSignature}>";
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
                process.Dispose();
            }

            Thread.Sleep(2000);
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
