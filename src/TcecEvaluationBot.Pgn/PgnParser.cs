namespace TcecEvaluationBot.Pgn
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class PgnParser
    {
        public GamesList ParseFromString(string inputString)
        {
            var games = new List<Game>();
            Game currentGame = null;

            using (var stringReader = new StringReader(inputString))
            {
                var inTags = false;

                var currentMove = 1;
                var currentColor = Color.White;

                string line;
                while ((line = stringReader.ReadLine()) != null)
                {
                    if (line.StartsWith("["))
                    {
                        // Tags
                        if (!inTags)
                        {
                            // We start a new game here
                            if (currentGame != null)
                            {
                                games.Add(currentGame);
                            }

                            currentGame = new Game();
                            inTags = true;
                            currentMove = 1;
                            currentColor = Color.White;
                        }

                        currentGame.Tags.Add(new Tag(line));
                    }
                    else
                    {
                        // Moves
                        inTags = false;
                        var moves = ParseMovesLine(line);
                        foreach (var move in moves)
                        {
                            if (move.Color == Color.Unknown)
                            {
                                move.Color = currentColor;
                            }

                            if (move.Number == 0)
                            {
                                move.Number = currentMove;
                            }

                            currentGame?.Moves.Add(move);

                            if (currentColor == Color.White)
                            {
                                currentColor = Color.Black;
                            }
                            else
                            {
                                currentColor = Color.White;
                                currentMove++;
                            }
                        }
                    }
                }
            }

            if (currentGame != null)
            {
                games.Add(currentGame);
            }

            return new GamesList(games);
        }

        private static IEnumerable<Move> ParseMovesLine(string line)
        {
            line = line.Trim();
            var moves = new List<Move>();
            var inComment = false;
            var comment = new StringBuilder();
            var moveInfo = new StringBuilder();
            foreach (var ch in line)
            {
                if (ch == '{' || ch == ';')
                {
                    inComment = true;
                    comment = new StringBuilder();
                    continue;
                }

                if (ch == '}')
                {
                    inComment = false;

                    var lastMove = moves.LastOrDefault();
                    if (lastMove != null)
                    {
                        lastMove.Comment = comment.ToString().Trim();
                    }

                    continue;
                }

                if (inComment)
                {
                    comment.Append(ch);
                    continue;
                }

                if (ch == ' ')
                {
                    if (moveInfo.Length == 0)
                    {
                        continue;
                    }

                    if (moveInfo[moveInfo.Length - 1] == '.')
                    {
                        moveInfo = new StringBuilder();
                        continue;
                    }

                    moves.Add(new Move { San = moveInfo.ToString().Trim() });
                    moveInfo = new StringBuilder();
                    continue;
                }

                moveInfo.Append(ch);
            }

            if (moveInfo.Length > 0 && moveInfo[moveInfo.Length - 1] != '.' && moveInfo.ToString() != "1/2-1/2" && moveInfo.ToString() != "1-0" && moveInfo.ToString() != "0-1")
            {
                moves.Add(new Move { San = moveInfo.ToString() });
            }

            return moves;
        }
    }
}
