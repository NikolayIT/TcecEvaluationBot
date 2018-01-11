namespace TcecEvaluationBot.ConsoleUI
{
    using System;

    using TcecEvaluationBot.ConsoleUI.Commands;

    using TwitchLib;
    using TwitchLib.Models.Client;

    public class EvaluationBot
    {
        private readonly Options options;

        private readonly TwitchClient twitchClient;

        private readonly EvalCommand evalCommand;

        private readonly TimeCommand timeCommand;

        public EvaluationBot(Options options)
        {
            this.options = options;
            var credentials = new ConnectionCredentials(options.TwitchUserName, options.TwitchAccessToken);
            this.twitchClient = new TwitchClient(credentials, options.TwitchChannelName);

            this.evalCommand = new EvalCommand(this.twitchClient, options);
            this.timeCommand = new TimeCommand(this.twitchClient, options);
        }

        public void Run()
        {
            this.twitchClient.OnConnected += (sender, arguments) => this.Log("Connected!");
            this.twitchClient.OnJoinedChannel += (sender, arguments) => this.Log($"Joined to {arguments.Channel}!");
            this.twitchClient.OnMessageReceived += (sender, arguments) =>
                {
                    if (arguments.ChatMessage.Message == "!eval"
                        || arguments.ChatMessage.Message.Trim().StartsWith("!eval "))
                    {
                        this.Log($"Received \"{arguments.ChatMessage.Message}\" from {arguments.ChatMessage.Username}");
                        var response = this.evalCommand.Execute(arguments.ChatMessage.Message);
                        this.twitchClient.SendMessage(response);
                        this.Log($"Responded with \"{response}\"");
                        
                    }
                    else if (arguments.ChatMessage.Message == "!time"
                             || arguments.ChatMessage.Message.Trim().StartsWith("!time "))
                    {
                        this.Log($"Received \"{arguments.ChatMessage.Message}\" from {arguments.ChatMessage.Username}");
                        var response = this.timeCommand.Execute(arguments.ChatMessage.Message);
                        this.twitchClient.SendMessage(response);
                        this.Log($"Responded with \"{response}\"");

                    }
                };
            this.twitchClient.Connect();
        }

        private void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.UtcNow}]");
            Console.ResetColor();
            Console.WriteLine($" {message}");
        }
    }
}
