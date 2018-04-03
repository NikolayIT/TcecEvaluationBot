namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Collections.Generic;

    using TcecEvaluationBot.ConsoleUI.Commands;

    using TwitchLib.Client;
    using TwitchLib.Client.Models;

    public class TwitchBot
    {
        private readonly Options options;

        private readonly TwitchClient twitchClient;

        private readonly IList<CommandInfo> commands = new List<CommandInfo>();

        public TwitchBot(Options options, Settings.Settings settings)
        {
            this.options = options;
            this.twitchClient = new TwitchClient();

            this.commands.Add(new CommandInfo("eval", new EvaluationCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("time", new TimeCommand(settings)));
            this.commands.Add(new CommandInfo("games", new GamesCommand(settings)));
            this.commands.Add(new CommandInfo("rand", new RandCommand()));
            this.commands.Add(new CommandInfo("db", new DbCommand()));
            this.commands.Add(new CommandInfo("position", new PositionCommand()));
            //// Console.WriteLine(new PositionCommand().Execute("!position")); Console.ReadLine();
        }

        public void Run()
        {
            var credentials = new ConnectionCredentials(this.options.TwitchUserName, this.options.TwitchAccessToken);
            this.twitchClient.Initialize(credentials, this.options.TwitchChannelName);
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
                            try
                            {
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
                            catch (Exception ex)
                            {
                                this.twitchClient.SendMessage($"[{DateTime.UtcNow:HH:mm:ss}] Error: {ex.Message}");
                                this.Log($"Error while executing \"{arguments.ChatMessage.Message}\": {ex.ToString()}");
                            }
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
