namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System;

    public static class GameResultHelper
    {
        public static string ToStringWordFormat(this GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWin:
                    return "win";
                case GameResult.BlackWin:
                    return "loss";
                case GameResult.Draw:
                    return "draw";
                default:
                    break;
            }

            throw new ArgumentException();
        }

        public static string ToStringPgnFormat(this GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWin:
                    return "1-0";
                case GameResult.BlackWin:
                    return "0-1";
                case GameResult.Draw:
                    return "1/2-1/2";
                default:
                    break;
            }

            throw new ArgumentException();
        }

        public static string ToStringPgnUnicodeFormat(this GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWin:
                    return "1-0";
                case GameResult.BlackWin:
                    return "0-1";
                case GameResult.Draw:
                    return "½-½";
                default:
                    break;
            }

            throw new ArgumentException();
        }

        public static string ToStringLetterFormat(this GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWin:
                    return "W";
                case GameResult.BlackWin:
                    return "L";
                case GameResult.Draw:
                    return "D";
                default:
                    break;
            }

            throw new ArgumentException();
        }

        public static Optional<GameResult> FromStringWordFormat(string str)
        {
            switch (str)
            {
                case "win":
                    return Optional<GameResult>.Create(GameResult.WhiteWin);
                case "loss":
                    return Optional<GameResult>.Create(GameResult.BlackWin);
                case "draw":
                    return Optional<GameResult>.Create(GameResult.Draw);
                default:
                    break;
            }

            return Optional<GameResult>.CreateEmpty();
        }

        public static Optional<GameResult> FromStringPgnFormat(string str)
        {
            switch (str)
            {
                case "1-0":
                    return Optional<GameResult>.Create(GameResult.WhiteWin);
                case "0-1":
                    return Optional<GameResult>.Create(GameResult.BlackWin);
                case "1/2-1/2":
                    return Optional<GameResult>.Create(GameResult.Draw);
                default:
                    break;
            }

            return Optional<GameResult>.CreateEmpty();
        }
    }
}
