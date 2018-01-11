namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TwitchLib;

    public class TimeCommand : ICommand
    {
        private readonly TwitchClient twitchClient;

        private readonly Options options;

        private readonly HttpClient httpClient;

        public TimeCommand(TwitchClient twitchClient, Options options)
        {
            this.twitchClient = twitchClient;
            this.options = options;
            this.httpClient = new HttpClient();
        }

        public string Execute(string message)
        {
            StringReader stringReader;
            try
            {
                var gamesInfoString =
                    this.GetTextContent(
                            "http://tcec.chessdom.com/archive/TCEC%20Season%2011%20-%20Division%203%20Schedule.txt")
                        .GetAwaiter()
                        .GetResult();

                if (string.IsNullOrWhiteSpace(gamesInfoString))
                {
                    throw new Exception();
                }

                stringReader = new StringReader(gamesInfoString);
            }
            catch (Exception)
            {
                return "Unable to retrieve schedule data.";
            }

            var header = stringReader.ReadLine();
            var startColumnIndex = header.IndexOf(" Start ", StringComparison.Ordinal) + 1;
            var durationColumnIndex = header.IndexOf(" Duration ", StringComparison.Ordinal) + 1;
            var ecoColumnIndex = header.IndexOf(" ECO ", StringComparison.Ordinal) + 1;
            string line;
            int countPlayed = 0;
            int count = 0;
            var totalTime = new TimeSpan();
            var lastStarted = (DateTime?)null;
            while ((line = stringReader.ReadLine()) != null)
            {
                count++;
                var time = line.Substring(durationColumnIndex, ecoColumnIndex - durationColumnIndex).Trim();
                if (!string.IsNullOrWhiteSpace(time))
                {
                    countPlayed++;
                    var timeParts = time.Split(':');
                    var timeSpan = new TimeSpan(0, int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));
                    totalTime += timeSpan;
                    //// Console.WriteLine(timeSpan);
                }
                else
                {
                    if (lastStarted == null)
                    {
                        var lastStartedAsString = line.Substring(startColumnIndex, durationColumnIndex - startColumnIndex).Trim();
                        if (DateTime.TryParseExact(
                            lastStartedAsString,
                            "HH:mm:ss on yyyy.MM.dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var parsedValue))
                        {
                            lastStarted = parsedValue;
                        }
                        else
                        {
                            lastStarted = DateTime.UtcNow;
                        }
                    }
                }
            }

            if (countPlayed == 0)
            {
                return "No games played.";
            }

            var averageGameTime = totalTime / countPlayed;
            var remainingTime = (count - countPlayed) * (averageGameTime + new TimeSpan(0, 0, 1, 0)); // +1 minute between games
            //// Console.WriteLine(lastStarted);
            //// Console.WriteLine(remainingTime);
            //// Console.WriteLine(lastStarted + remainingTime);
            //// Console.WriteLine($"\"{totalTime / countPlayed}\"");
            var response =
                $"[{DateTime.UtcNow:HH:mm:ss}] {count - countPlayed} games left. Average duration: {(totalTime / countPlayed):hh\\:mm\\:ss}. Estimated division end: {lastStarted + remainingTime:R}.";
            return response;
        }

        private async Task<string> GetTextContent(string url)
        {
            var response = await this.httpClient.GetAsync($"{url}?noCache={Guid.NewGuid()}");
            var stringResult = await response.Content.ReadAsStringAsync();
            return stringResult;
        }
    }
}
