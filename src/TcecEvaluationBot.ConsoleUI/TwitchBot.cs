namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Collections.Generic;

    using TcecEvaluationBot.ConsoleUI.Commands;
    using TcecEvaluationBot.ConsoleUI.Services;

    using TwitchLib.Client;
    using TwitchLib.Client.Models;

    public class TwitchBot
    {
        private readonly Options options;

        private readonly TwitchClient twitchClient;

        private readonly ILogger logger;

        private readonly IList<CommandInfo> commands = new List<CommandInfo>();

        public TwitchBot(Options options, Settings.Settings settings)
        {
            this.options = options;
            this.twitchClient = new TwitchClient();
            this.logger = new FileLogger($"log_{DateTime.UtcNow:yyyy-MM-dd}.txt");

            this.commands.Add(new CommandInfo("eval", new EvaluationCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("time", new TimeCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("games", new GamesCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("rand", new RandCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("db", new DbCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("static", new StaticCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("reverse", new ReverseCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("evalhelp", new EvalHelpCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("evalengines", new EvalEnginesCommand(this.twitchClient, options, settings)));
            //// Console.WriteLine(new EvaluationCommand(this.twitchClient, options, settings).Execute("!eval 5")); Console.ReadLine();
        }

        public void Run()
        {
            string lastMessage = null;
            var credentials = new ConnectionCredentials(this.options.TwitchUserName, this.options.TwitchAccessToken);
            this.twitchClient.Initialize(credentials, this.options.TwitchChannelName);
            this.twitchClient.OnConnected += (sender, arguments) => this.Log("Connected!");
            this.twitchClient.OnJoinedChannel += (sender, arguments) => this.Log($"Joined to {arguments.Channel}!");
            this.twitchClient.OnMessageReceived += (sender, arguments) =>
                {
                    this.logger.Log($"RECEIVED: {arguments.ChatMessage.Username}: {arguments.ChatMessage.Message}");
                    foreach (var command in this.commands)
                    {
                        if (arguments.ChatMessage.Message == $"!{command.Text}"
                            || arguments.ChatMessage.Message.Trim().StartsWith($"!{command.Text} "))
                        {
                            this.Log($"Received \"{arguments.ChatMessage.Message}\" from {arguments.ChatMessage.Username}");
                            try
                            {
                                string message;
                                var cooldownRemaining = this.options.CooldownTime - (DateTime.UtcNow - command.LastMessage).TotalSeconds;
                                if (cooldownRemaining >= 0.1)
                                {
                                    message = $"[{DateTime.UtcNow:HH:mm:ss}] \"!{command.Text}\" will be available in {cooldownRemaining:0.0} sec.";
                                    this.twitchClient.SendMessage(this.options.TwitchChannelName, message);
                                    this.logger.Log($"SENT: {message}");
                                }
                                else
                                {
                                    command.LastMessage = DateTime.UtcNow;
                                    message = command.Command.Execute(arguments.ChatMessage.Message);
                                    var messageToSend =
                                        message != lastMessage
                                            ? $"/me {message}"
                                            : $"/me [{DateTime.UtcNow:HH:mm:ss}] {message}";
                                    this.twitchClient.SendMessage(this.options.TwitchChannelName, messageToSend);
                                    this.logger.Log($"SENT: {messageToSend}");
                                    lastMessage = message;
                                }

                                this.Log($"Responded with \"{message}\"");
                            }
                            catch (Exception ex)
                            {
                                this.logger.Log($"ERROR: {ex}");
                                this.twitchClient.SendMessage(this.options.TwitchChannelName, $"[{DateTime.UtcNow:HH:mm:ss}] Error: {ex.Message}");
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
