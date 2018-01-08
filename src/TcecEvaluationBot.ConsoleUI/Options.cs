namespace TcecEvaluationBot.ConsoleUI
{
    using CommandLine;

    internal class Options
    {
        [Option('u', "twitchUserName", Required = true, HelpText = "Twitch username for the chat bot.")]
        public string TwitchUserName { get; set; }

        [Option('a', "twitchAccessToken", Required = true, HelpText = "Twitch access token. You can generate one from twitchtokengenerator.com")]
        public string TwitchAccessToken { get; set; }

        [Option('s', "syzygyPath", Required = false, HelpText = "The path for syzygy table base.")]
        public string SyzygyPath { get; set; }
    }
}
