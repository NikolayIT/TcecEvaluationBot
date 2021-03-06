﻿namespace TcecEvaluationBot.ConsoleUI.Commands
{
    using System;

    using TcecEvaluationBot.ConsoleUI.Settings;

    using TwitchLib.Client;

    public class RandCommand : BaseCommand
    {
        private readonly Random random;

        public RandCommand(TwitchClient twitchClient, Options options, Settings settings)
            : base(twitchClient, options, settings)
        {
            this.random = new Random();
        }

        public override string Execute(string message)
        {
            var parts = message.Split(" ");
            if (parts.Length >= 3 && long.TryParse(parts[1], out var firstValue) && long.TryParse(parts[2], out var secondValue))
            {
                var randomNumber = this.LongRandom(Math.Min(firstValue, secondValue), Math.Max(firstValue, secondValue) + 1);
                return $"Random number [{Math.Min(firstValue, secondValue)}; {Math.Max(firstValue, secondValue)}]: {randomNumber}";
            }

            return "Usage: !rand [minNumber] [maxNumber]";
        }

        public long LongRandom(long min, long max)
        {
            var buf = new byte[8];
            this.random.NextBytes(buf);
            var longRand = BitConverter.ToInt64(buf, 0);
            return Math.Abs(longRand % (max - min)) + min;
        }
    }
}
