namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class TimeCommand : ICommand
    {
        private readonly HttpClient httpClient;

        public TimeCommand()
        {
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

            var games = this.ReadGames(stringReader);

            var countPlayed = games.Count(x => x.IsPlayed);
            var totalTime = games.Where(x => x.Duration.HasValue).Aggregate(TimeSpan.Zero, (sumSoFar, x) => sumSoFar + x.Duration.Value);
            var lastStarted = games.Where(x => x.Started.HasValue).Select(x => x.Started.Value)
                .OrderByDescending(x => x).FirstOrDefault();
            if (countPlayed == 0)
            {
                return "No games played.";
            }

            var averageGameTime = totalTime / countPlayed;
            var remainingTime = (games.Count - countPlayed) * (averageGameTime + new TimeSpan(0, 0, 1, 0)); // +1 minute between games
            var response =
                $"[{DateTime.UtcNow:HH:mm:ss}] {games.Count - countPlayed} games left. Average duration: {(totalTime / countPlayed):hh\\:mm\\:ss}. Estimated division end: {lastStarted + remainingTime:R}.";
            return response;
        }

        private IList<Game> ReadGames(StringReader stringReader)
        {
            // Columns
            var header = stringReader.ReadLine();
            if (header == null)
            {
                return new List<Game>();
            }

            var whiteColumnIndex = header.IndexOf(" White ", StringComparison.Ordinal) + 1;
            var blackColumnIndex = header.IndexOf(" Black ", StringComparison.Ordinal) + 1;
            var startColumnIndex = header.IndexOf(" Start ", StringComparison.Ordinal) + 1;
            var durationColumnIndex = header.IndexOf(" Duration ", StringComparison.Ordinal) + 1;
            var ecoColumnIndex = header.IndexOf(" ECO ", StringComparison.Ordinal) + 1;

            var games = new List<Game>();
            string line;
            int gameIndex = 0;

            while ((line = stringReader.ReadLine()) != null)
            {
                gameIndex++;
                var game = new Game { Number = gameIndex };

                var durationText = line.Substring(durationColumnIndex, ecoColumnIndex - durationColumnIndex).Trim();
                if (!string.IsNullOrWhiteSpace(durationText))
                {
                    var timeParts = durationText.Split(':');
                    var duration = new TimeSpan(0, int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));
                    game.Duration = duration;
                    //// Console.WriteLine(timeSpan);
                }

                var lastStartedAsString = line.Substring(startColumnIndex, durationColumnIndex - startColumnIndex).Trim();
                if (DateTime.TryParseExact(
                    lastStartedAsString,
                    "HH:mm:ss on yyyy.MM.dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedValue))
                {
                    game.Started = parsedValue;
                }

                games.Add(game);
            }

            return games;
        }

        private async Task<string> GetTextContent(string url)
        {
            var response = await this.httpClient.GetAsync($"{url}?noCache={Guid.NewGuid()}");
            var stringResult = await response.Content.ReadAsStringAsync();
            return stringResult;
        }

        private class Game
        {
            public int Number { get; set; }

            public TimeSpan? Duration { get; set; }

            public DateTime? Started { get; set; }

            public bool IsPlayed => this.Duration.HasValue;
        }
    }
}
