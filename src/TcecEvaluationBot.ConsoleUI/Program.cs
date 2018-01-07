namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TwitchLib;
    using TwitchLib.Models.Client;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine($"Usage: {typeof(Program).Assembly.GetName().Name}.exe [twitchUserName] [twitchAccessToken]");
                Console.WriteLine($"You can generate an access token from twitchtokengenerator.com");
                return;
            }

            var twitchUserName = args[0];
            var twitchAccessToken = args[1];

            var random = new Random();
            var sw = Stopwatch.StartNew();
            var client = new HttpClient();
            var response = await client.GetAsync("http://tcec.chessdom.com/live/live.pgn?noCache=" + random.Next());
            var stringResult = await response.Content.ReadAsStringAsync();

            File.WriteAllText("file.pgn", stringResult);

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

            var line = string.Empty;
            while (!process.StandardOutput.EndOfStream)
            {
                var currentLine = process.StandardOutput.ReadLine();
                if (currentLine != string.Empty && currentLine != "*")
                {
                    line = currentLine;
                    Console.WriteLine(line);
                }
            }

            var outputParts = line.Split("\"");
            var fenPosition = string.Empty;
            if (outputParts.Length > 2)
            {
                fenPosition = outputParts[1];
                Console.WriteLine(fenPosition);
            }

            Console.WriteLine(sw.Elapsed);

            var sfProcess = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                                    {
                                                        FileName = "stockfish.exe",
                                                        UseShellExecute = false,
                                                        RedirectStandardOutput = true,
                                                        RedirectStandardInput = true,
                                                        CreateNoWindow = true
                                                    }
                                };
            sfProcess.Start();

            sfProcess.StandardInput.WriteLine($"position fen \"{fenPosition}\"");
            sfProcess.StandardInput.WriteLine($"go movetime 1000");

            while (!sfProcess.StandardOutput.EndOfStream)
            {
                var currentLine = sfProcess.StandardOutput.ReadLine();
                if (currentLine != string.Empty)
                {
                    line = currentLine;
                    Console.WriteLine(line);
                    if (line.StartsWith("bestmove"))
                    {
                        break;
                    }
                }
            }

            // twitchtokengenerator.com
            var credentials = new ConnectionCredentials(twitchUserName, twitchAccessToken);
            var twitchClient = new TwitchClient(credentials, "tcecpoc");
            twitchClient.OnConnected += (sender, arguments) => twitchClient.SendMessage(line);
            twitchClient.OnJoinedChannel += (sender, arguments) => Console.WriteLine(arguments.Channel);
            twitchClient.OnMessageReceived += (sender, arguments) => Console.WriteLine(arguments.ChatMessage.Message);
            twitchClient.Connect();
            Console.WriteLine(line);
            Console.ReadLine();
        }
    }
}
