namespace TcecEvaluationBot.ConsoleUI
{
    using System;

    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine($"Usage: {typeof(Program).Assembly.GetName().Name}.exe [twitchUserName] [twitchAccessToken]");
                Console.WriteLine($"You can generate an access token from twitchtokengenerator.com");
                return;
            }

            var twitchUserName = args[0];
            var twitchAccessToken = args[1];

            var bot = new EvaluationBot(twitchUserName, twitchAccessToken);
            bot.Run();
            Console.ReadLine();
        }
    }
}
