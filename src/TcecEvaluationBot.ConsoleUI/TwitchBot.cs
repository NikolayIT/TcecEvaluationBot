namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Collections.Generic;

    using TcecEvaluationBot.ConsoleUI.Commands;

    using TwitchLib;
    using TwitchLib.Models.Client;

    public class TwitchBot
    {
        private readonly Options options;

        private readonly TwitchClient twitchClient;

        private readonly IList<CommandInfo> commands = new List<CommandInfo>();

        public TwitchBot(Options options, Settings.Settings settings)
        {
            this.options = options;
            var credentials = new ConnectionCredentials(options.TwitchUserName, options.TwitchAccessToken);
            this.twitchClient = new TwitchClient(credentials, options.TwitchChannelName);
            this.commands.Add(new CommandInfo("eval", new EvaluationCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("time", new TimeCommand(settings)));
            this.commands.Add(new CommandInfo("games", new GamesCommand(settings)));
            this.commands.Add(new CommandInfo("rand", new RandCommand()));
            //// Console.WriteLine(new EvaluationCommand(this.twitchClient, options, settings).Execute("!eval lczero"));
            //// Console.ReadLine();
        }

        public void Run()
        {
            this.twitchClient.OnConnected += (sender, arguments) => this.Log("Connected!");
            this.twitchClient.OnJoinedChannel += (sender, arguments) => this.Log($"Joined to {arguments.Channel}!");
            this.twitchClient.OnMessageReceived += (sender, arguments) =>
                {
                    foreach (var command in this.commands)
                    {
                        if (arguments.ChatMessage.Message == $"!{command.Text}"
                            || arguments.ChatMessage.Message.Trim().StartsWith($"!{command.Text} "))
                        {
                            this.Log($"Received \"{arguments.ChatMessage.Message}\" from {arguments.ChatMessage.Username}");

                            string message;
                            if ((DateTime.UtcNow - command.LastMessage).TotalSeconds < this.options.CooldownTime)
                            {
                                var cooldownRemaining = this.options.CooldownTime - (DateTime.UtcNow - command.LastMessage).TotalSeconds;
                                message = $"[{DateTime.UtcNow:HH:mm:ss}] \"!{command.Text}\" will be available in {cooldownRemaining:0.0} sec.";
                            }
                            else
                            {
                                command.LastMessage = DateTime.UtcNow;
                                message = command.Command.Execute(arguments.ChatMessage.Message);
                            }

                            this.twitchClient.SendMessage(message);
                            this.Log($"Responded with \"{message}\"");
                        }
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

        private class CommandInfo
        {
            public CommandInfo(string text, ICommand command)
            {
                this.Text = text;
                this.Command = command;
                this.LastMessage = DateTime.UtcNow.AddDays(-1);
            }

            public string Text { get; }

            public ICommand Command { get; }

            public DateTime LastMessage { get; set; }
        }
    }
}
