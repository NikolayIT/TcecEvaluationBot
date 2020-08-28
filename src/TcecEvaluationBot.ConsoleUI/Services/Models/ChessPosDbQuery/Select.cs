using System;

namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    public enum Select { Continuations, Transpositions, All };

    public static class SelectHelper
    {
        public static Select[] Values = { Select.Continuations, Select.Transpositions, Select.All };

        public static string Stringify(this Select result)
        {
            switch (result)
            {
                case Select.Continuations:
                    return "continuations";
                case Select.Transpositions:
                    return "transpositions";
                case Select.All:
                    return "all";
            }

            throw new ArgumentException();
        }

        public static Optional<Select> FromString(string str)
        {
            switch (str)
            {
                case "continuations":
                    return Optional<Select>.Create(Select.Continuations);
                case "transpositions":
                    return Optional<Select>.Create(Select.Transpositions);
                case "all":
                    return Optional<Select>.Create(Select.All);
            }

            return Optional<Select>.CreateEmpty();
        }
    }
}
