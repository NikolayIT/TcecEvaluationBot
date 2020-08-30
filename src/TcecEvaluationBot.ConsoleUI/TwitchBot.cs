namespace TcecEvaluationBot.ConsoleUI
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using TcecEvaluationBot.ConsoleUI.Commands;
    using TcecEvaluationBot.ConsoleUI.Services;

    using TwitchLib.Client;
    using TwitchLib.Client.Models;

    public class TwitchBot : IDisposable
    {
        private readonly Options options;

        private readonly Settings.Settings settings;

        private readonly TwitchClient twitchClient;

        private readonly ILogger logger;

        private readonly IList<CommandInfo> commands = new List<CommandInfo>();

        public TwitchBot(Options options, Settings.Settings settings)
        {
            this.options = options;
            this.settings = settings;
            this.twitchClient = new TwitchClient();
            this.logger = new FileLogger($"log_{DateTime.UtcNow:yyyy-MM-dd}.txt");

            this.commands.Add(new CommandInfo("eval", new EvalCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("static", new StaticCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("time", new TimeCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("games", new GamesCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("rand", new RandCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("calc", new CalcCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("db", new DbCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("tb", new TbCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("links", new LinksCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("reverse", new ReverseCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("evalhelp", new EvalHelpCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("evalengines", new EvalEnginesCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("temp", new TempCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("outputmoveson", new SetOutputMovesCommand(this.twitchClient, options, settings, true)));
            this.commands.Add(new CommandInfo("outputmovesoff", new SetOutputMovesCommand(this.twitchClient, options, settings, false)));
            this.commands.Add(new CommandInfo("iccfdb", new IccfDbCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("define", new DefineCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("urban", new UrbanCommand(this.twitchClient, options, settings)));
            this.commands.Add(new CommandInfo("dbcn", new ChessDbcnCommand(this.twitchClient, options, settings)));

            //// Console.WriteLine(new DefineCommand(this.twitchClient, options, settings).Execute("!define test")); Console.ReadLine();
        }

        public Task OutputMovesTask()
        {
            return Task.Run(
                () =>
                    {
                        Thread.Sleep(5000);
                        Console.WriteLine("Output moves task is running...");
                        var infoProvider = new CurrentGameInfoProvider(this.settings.LivePgnUrl);
                        var lastFen = string.Empty;
                        while (true)
                        {
                            if (this.settings.OutputMoves)
                            {
                                var info = infoProvider.GetInfo();
                                if (!string.IsNullOrWhiteSpace(info.Fen) &&
                                    !string.IsNullOrWhiteSpace(info.LastMove) &&
                                    info.Fen != lastFen)
                                {
                                    lastFen = info.Fen;
                                    var message = $"New move: {info.LastMove}";
                                    this.twitchClient.SendMessage(
                                        this.options.TwitchChannelName,
                                        $"/me [{DateTime.UtcNow:HH:mm:ss}] {message}");
                                }
                            }

                            Thread.Sleep(2000);
                        }
                    });
        }

        public void Run()
        {
            Console.WriteLine("Commands receiver is running...");
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
                                this.Log($"Error while executing \"{arguments.ChatMessage.Message}\": {ex}");
                            }
                        }
                    }
                };
            this.twitchClient.Connect();
        }

        public void Dispose()
        {
            foreach (var command in this.commands)
            {
                command.Command.Dispose();
            }

            this.commands.Clear();
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
