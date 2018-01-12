# TcecEvaluationBot
Position evaluation chat bot for http://tcec.chessdom.com/live.php

## Command-line parameters

    -u, --twitchUserName       Required. Twitch username for the chat bot.
    -a, --twitchAccessToken    Required. Twitch access token. You can generate one from twitchtokengenerator.com
    -c, --twitchChannelName    Required. The name of the Twitch chat channel.
    -s, --syzygyPath           The path for syzygy table base.
    -m, --moveTime             (Default: 10000) Time (in milliseconds) for the engine to think.
    -t, --threads              (Default: 2) The number of threads for the engine to run on.
    -h, --hash                 (Default: 128) The size of the hash (in MB) to be used by the engine.
    --cooldownTime             (Default: 30) Cooldown time (in seconds) for the evaluation command.
    --thinkingMessage          (Default: true) Show thinking message before starting the evaluation.
    --help                     Display this help screen.
    --version                  Display version information.