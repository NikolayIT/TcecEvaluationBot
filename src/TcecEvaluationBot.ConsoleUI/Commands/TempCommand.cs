namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;

    using TcecEvaluationBot.ConsoleUI.Services;
    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class TempCommand : BaseCommand
    {
        private HttpClient httpClient;

        public TempCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.httpClient = new HttpClient();
        }

        public override string Execute(string message)
        {
            try
            {
                // CPU
                var cpuTempInfo = this.GetString("https://tcec-chess.com/cpu_temperature.txt");
                var cpuLines = Regex.Split(cpuTempInfo, "\r\n|\r|\n");
                var cpuTemperatures = new List<int>();
                for (var i = 1; i <= 4; i++)
                {
                    var parts = cpuLines[i].Split('|');
                    var temperature = int.Parse(parts[1].Replace("degrees C", string.Empty).Trim());
                    cpuTemperatures.Add(temperature);
                }

                // GPU
                var gpuTempInfo = this.GetString("https://tcec-chess.com/gpu_temperature.txt");
                var gpuLines = Regex.Split(gpuTempInfo, "\r\n|\r|\n");
                var gpuTemperatures = new List<int>();
                for (var i = 3; i <= 6; i++)
                {
                    var temperature = int.Parse(gpuLines[i].Substring(12, 5).Trim());
                    gpuTemperatures.Add(temperature);
                }

                return
                    $"CPU {cpuTemperatures.Average():0.0}°C ({string.Join(",", cpuTemperatures)}) tcec-chess.com/cpu_temperature.txt • GPU {gpuTemperatures.Average():0.0}°C ({string.Join(",", gpuTemperatures)}) tcec-chess.com/gpu_temperature.txt • Updated every 5min.";
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }

        private string GetString(string url)
        {
            var response = this.httpClient.GetAsync(url).GetAwaiter().GetResult();
            var stringContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return stringContent;
        }
    }
}
