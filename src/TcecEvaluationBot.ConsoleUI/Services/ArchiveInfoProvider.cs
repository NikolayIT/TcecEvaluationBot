namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System.Linq;
    using System.Net.Http;

    using TcecEvaluationBot.Pgn;

    public class ArchiveInfoProvider
    {
        private readonly string currentGamePgn;

        private readonly string archivePgnUrl;

        private readonly HttpClient httpClient;

        private readonly PgnParser pgnParser;

        public ArchiveInfoProvider(string currentGamePgn, string archivePgnUrl)
        {
            this.currentGamePgn = currentGamePgn;
            this.archivePgnUrl = archivePgnUrl;
            this.httpClient = new HttpClient();
            this.pgnParser = new PgnParser();
        }

        public GamesList GetGames()
        {
            var pgnResponse = this.httpClient.GetAsync(this.archivePgnUrl).GetAwaiter().GetResult();
            var pgn = pgnResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var gamesList = this.pgnParser.ParseFromString(pgn);
            return gamesList;
        }

        public Game GetCurrentGame()
        {
            var pgnResponse = this.httpClient.GetAsync(this.currentGamePgn).GetAwaiter().GetResult();
            var pgn = pgnResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var gamesList = this.pgnParser.ParseFromString(pgn);
            return gamesList.Games.First();
        }
    }
}
