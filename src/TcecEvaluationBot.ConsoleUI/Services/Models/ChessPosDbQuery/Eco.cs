namespace TcecEvaluationBot.ConsoleUI.Services.Models.ChessPosDbQuery
{
    using System;

    using Newtonsoft.Json.Linq;

    public struct Eco
    {
        public Eco(char category, byte index)
        {
            this.Category = category;
            this.Index = index;
        }

        public char Category { get; set; }

        public byte Index { get; set; }

        public static Eco FromJson(JToken json)
        {
            return Eco.FromString(json.Value<string>());
        }

        public static Eco FromString(string str)
        {
            if (str.Length != 3)
            {
                throw new ArgumentException();
            }

            if (str[0] < 'A' || str[0] > 'E')
            {
                throw new ArgumentException();
            }

            return new Eco
            {
                Category = str[0],
                Index = byte.Parse(str.Substring(1, 2)),
            };
        }

        public override string ToString()
        {
            return this.Category + this.Index.ToString("D2");
        }
    }
}
