namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    using TcecEvaluationBot.ConsoleUI.Services.Models;

    public class GamesInfoProvider
    {
        private readonly string scheduleUrl;

        private readonly HttpClient httpClient;

        public GamesInfoProvider(string scheduleUrl)
        {
            this.scheduleUrl = scheduleUrl;
            this.httpClient = new HttpClient();
        }

        public GamesList GetGames()
        {
            var gamesInfoString = this.GetTextContent(this.scheduleUrl).GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(gamesInfoString))
            {
                throw new Exception();
            }

            var stringReader = new StringReader(gamesInfoString);

            var games = this.ReadGamesFromStringReader(stringReader);
            return new GamesList(games);
        }

        private IList<Game> ReadGamesFromStringReader(StringReader stringReader)
        {
            // Columns
            var header = stringReader.ReadLine();
            if (header == null)
            {
                return new List<Game>();
            }

            var numberColumnIndex = header.IndexOf("Nr ", StringComparison.Ordinal) + 1;
            var whiteColumnIndex = header.IndexOf(" White ", StringComparison.Ordinal) + 1;
            var blackColumnIndex = header.IndexOf(" Black ", StringComparison.Ordinal) + 1;
            var resultColumnIndex = whiteColumnIndex + "White".Length;
            var terminationColumnIndex = header.IndexOf(" Termination ", StringComparison.Ordinal) + 1;
            var startColumnIndex = header.IndexOf(" Start ", StringComparison.Ordinal) + 1;
            var durationColumnIndex = header.IndexOf(" Duration ", StringComparison.Ordinal) + 1;
            var ecoColumnIndex = header.IndexOf(" ECO ", StringComparison.Ordinal) + 1;

            var games = new List<Game>();
            string line;
            var gameIndex = 0;

            while ((line = stringReader.ReadLine()) != null)
            {
                gameIndex++;
                var game = new Game { Number = gameIndex };

                var durationText = line.Substring(durationColumnIndex, ecoColumnIndex - durationColumnIndex).Trim();
                if (!string.IsNullOrWhiteSpace(durationText))
                {
                    var timeParts = durationText.Split(':');
                    var duration = new TimeSpan(
                        0,
                        int.Parse(timeParts[0]),
                        int.Parse(timeParts[1]),
                        int.Parse(timeParts[2]));
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
                    game.Started = parsedValue.AddHours(-2);
                }

                var whiteText = line.Substring(numberColumnIndex + 2, whiteColumnIndex - numberColumnIndex + 4).Trim();
                game.WhiteName = whiteText.Trim();

                var blackText = line.Substring(blackColumnIndex, terminationColumnIndex - blackColumnIndex).Trim();
                game.BlackName = blackText.Trim();

                var resultText = line.Substring(resultColumnIndex, blackColumnIndex - resultColumnIndex).Trim();
                game.Result = resultText.Trim();

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
    }
}
