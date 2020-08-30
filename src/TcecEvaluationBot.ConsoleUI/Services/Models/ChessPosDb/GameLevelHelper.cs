namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System;

    public static class GameLevelHelper
    {
        public static string Stringify(this GameLevel result)
        {
            switch (result)
            {
                case GameLevel.Human:
                    return "human";
                case GameLevel.Engine:
                    return "engine";
                case GameLevel.Server:
                    return "server";
                default:
                    break;
            }

            throw new ArgumentException();
        }

        public static Optional<GameLevel> FromString(string str)
        {
            switch (str)
            {
                case "human":
                    return Optional<GameLevel>.Create(GameLevel.Human);
                case "engine":
                    return Optional<GameLevel>.Create(GameLevel.Engine);
                case "server":
                    return Optional<GameLevel>.Create(GameLevel.Server);
                default:
                    break;
            }

            return Optional<GameLevel>.CreateEmpty();
        }
    }
}
