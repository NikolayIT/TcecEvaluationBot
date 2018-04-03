namespace TcecEvaluationBot.ConsoleUI.Services
{
    using System;

    public static class FenHelpers
    {
        public static string GetMoveNumberFromFen(this string fen)
        {
            var fenParts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var moveNumber = fenParts[fenParts.Length - 1].Trim();
            return moveNumber;
        }

        public static string GetPlayerToMoveFromFen(this string fen)
        {
            return fen.Contains(" b ") ? "b" : "w";
        }

        public static string GetMoveInfoFromFen(this string fen)
        {
            return GetMoveNumberFromFen(fen) + GetPlayerToMoveFromFen(fen);
        }
    }
}
