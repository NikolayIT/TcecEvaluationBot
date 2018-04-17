namespace TcecEvaluationBot.Pgn
{
    using System.Collections.Generic;
    using System.IO;

    public class PgnParser
    {
        public GamesList ParseFromString(string inputString)
        {
            var games = new List<Game>();
            var inTags = false;
            Game currentGame = null;
            var stringReader = new StringReader(inputString);
            string line;
            while ((line = stringReader.ReadLine()) != null)
            {
                if (line.StartsWith("["))
                {
                    if (!inTags)
                    {
                        // We start a new game here
                        if (currentGame != null)
                        {
                            games.Add(currentGame);
                        }

                        currentGame = new Game();
                        inTags = true;
                    }

                    currentGame.Tags.Add(new Tag(line));
                }
                else
                {
                    inTags = false;
                }
            }

            return new GamesList(games);
        }
    }
}
