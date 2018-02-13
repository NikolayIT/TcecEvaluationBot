namespace TcecEvaluationBot.ConsoleUI
{
    using CommandLine;

    public class Options
    {
        [Option('u', "twitchUserName", Required = true, HelpText = "Twitch username for the chat bot.")]
        public string TwitchUserName { get; set; }

        [Option('a', "twitchAccessToken", Required = true, HelpText = "Twitch access token. You can generate one from twitchtokengenerator.com")]
        public string TwitchAccessToken { get; set; }

        [Option('c', "twitchChannelName", Required = true, HelpText = "The name of the Twitch chat channel.")]
        public string TwitchChannelName { get; set; }

        [Option('s', "syzygyPath", Required = false, HelpText = "The path for syzygy table base.")]
        public string SyzygyPath { get; set; }

        [Option('m', "moveTime", Default = 10000, HelpText = "Time (in milliseconds) for the engine to think.")]
        public int MoveTime { get; set; }

        [Option('t', "threads", Default = 2, HelpText = "The number of threads for the engine to run on.")]
        public int Threads { get; set; }

        [Option('h', "hash", Default = 128, HelpText = "The size of the hash (in MB) to be used by the engine.")]
        public int HashSize { get; set; }

        [Option("cooldownTime", Default = 30, HelpText = "Cooldown time (in seconds) for the evaluation command.")]
        public int CooldownTime { get; set; }

        [Option("thinkingMessage", Default = true, HelpText = "Show thinking message before starting the evaluation.")]
        public bool ThinkingMessage { get; set; }

        [Option("contempt", Default = 0, HelpText = "The contempt value for chess engines.")]
        public int Contempt { get; set; }
    }
}
