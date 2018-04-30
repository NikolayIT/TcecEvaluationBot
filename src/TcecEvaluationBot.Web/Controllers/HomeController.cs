namespace TcecEvaluationBot.Web.Controllers
{
    using System.Diagnostics;
    using System.Net.Http;

    using Microsoft.AspNetCore.Mvc;

    using TcecEvaluationBot.Pgn;
    using TcecEvaluationBot.Web.Models;

    public class HomeController : Controller
    {
        private readonly HttpClient httpClient;

        private readonly PgnParser pgnParser;

        public HomeController()
        {
            this.httpClient = new HttpClient();
            this.pgnParser = new PgnParser();
        }

        [ResponseCache(Duration = 30)]
        public IActionResult Index()
        {
            var games = this.GetGames();
            return this.View(games);
        }

        [ResponseCache(Duration = 30)]
        public IActionResult Crosstable()
        {
            var data = this.httpClient
                .GetAsync(
                    "http://tcec.chessdom.com/archive/TCEC%20Season%2012%20-%20Division%204%20Crosstable.txt")
                .GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return this.Content(data);
        }

        [ResponseCache(Duration = 30)]
        public IActionResult Schedule()
        {
            var data = this.httpClient
                .GetAsync(
                    "http://tcec.chessdom.com/archive/TCEC%20Season%2012%20-%20Division%204%20Schedule.txt")
                .GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return this.Content(data);
        }

        [ResponseCache(Duration = 3)]
        public IActionResult LivePgn()
        {
            var data = this.httpClient
                .GetAsync(
                    "http://tcec.chessdom.com/live/live.pgn")
                .GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return this.Content(data);
        }

        public IActionResult Error()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }

        private GamesList GetGames()
        {
            var pgnResponse = this.httpClient.GetAsync("http://tcec.chessdom.com/dl.php?live=2").GetAwaiter().GetResult();
            var pgn = pgnResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var gamesList = this.pgnParser.ParseFromString(pgn);
            return gamesList;
        }
    }
}
