namespace TcecEvaluationBot.ConsoleUI
{
    using System;

    using CommandLine;

    using TcecEvaluationBot.ConsoleUI.Settings;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var settingsParser = new SettingsParser();
            var settings = settingsParser.ParseSettings("appsettings.json");

            var parserResult = Parser.Default.ParseArguments<Options>(args);
            parserResult.WithParsed(options => RunBot(options, settings));
        }

        private static void RunBot(Options options, Settings.Settings settings)
        {
            var bot = new TwitchBot(options, settings);
            bot.Run();
            Console.ReadLine();
        }
    }
}
