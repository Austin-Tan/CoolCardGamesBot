using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Games;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot
{
    public enum DiscardGames
    {
        NoThanks,
        IncanGold,
        SixNimmt,
        NOTFOUND
    }

    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        // Dictionary mapping channels to active games
        public Dictionary<ulong, ICardGame> activeGames;

        public async Task MainAsync()
        {

            DiscordSocketConfig config = new DiscordSocketConfig { MessageCacheSize = 64, GuildSubscriptions = false};
            // When Discord.Net.WebSocket receives a stable update to include GatewayIntents, add here
            // and unsub from typing, etc. instead of guildsubscriptions

            _client = new DiscordSocketClient(config);
            _client.MessageReceived += MessageHandler;
            _client.Log += Log;

            //  You can assign your bot token to a string, and pass that in to connect.


            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            // Copy!Paste your token in Environment Variables under User.
            // Can select System!wide Env Variables instead of User, just change the 2nd Param for targeting.
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN", EnvironmentVariableTarget.User);
            // Console.WriteLine("token is :" + token);

            activeGames = new Dictionary<ulong, ICardGame>();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetActivityAsync(new Game("!help", ActivityType.Listening, ActivityProperties.Spectate, "details??"));

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine("Log delegate called");
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task RouteMessage(string command, SocketMessage message)
        {
            string author = message.Author.Username;
            Console.WriteLine($"{author} asked:\n{command}");

            if (command == "help")
            {
                EmbedBuilder builder = new EmbedBuilder
                {
                    Title = "HELP - Cool Card Games Bot",
                    Description = "List of commands:",
                };
                builder.WithAuthor(_client.CurrentUser)
                    .AddField("!help", "gives you this messsage!")
                    .AddField("!play GameName", "WIP. Hopefully starts a game of GameName for you.")
                    .AddField("!echo abcdefg...", "makes me say something")
                    .AddField("!bugsnax", "try it for a surprise! 1% chance of losing all your primogems")
                    .AddField("List of games for !play:", "NoThanks")
                    .WithColor(Color.Orange);
                Embed msg = builder.Build();

                message.Channel.SendMessageAsync(null, false, msg);
            } else if (command == "bugsnax")
            {
                message.Channel.SendMessageAsync("it's bugsnax, " + message.Author.Mention);
                message.AddReactionAsync(new Emoji("\U0001f495"));

                Task<RestUserMessage> sent = message.Channel.SendFileAsync("bugsnax.jpg");


                sent.Result.AddReactionsAsync(new IEmote[] { new Emoji("🇧"), new Emoji("🇺"), new Emoji("🇬") });
            } else if (command.Split(' ')[0] == "echo")
            {
                string toSend = command.Substring(command.IndexOf("echo ") + "echo ".Length);
                message.Channel.SendMessageAsync(toSend + "\n- " + message.Author.Mention);
            } else if (command.Split(' ')[0] == "play")
            {
                // find right game/validate string
                string gameName = command.Substring(command.IndexOf("play ") + "play ".Length);
                DiscardGames newGame = findGameFromString(gameName);

                // make sure it's an actually implemented game
                if (newGame == DiscardGames.NOTFOUND)
                {
                    message.Channel.SendMessageAsync("Sorry, I couldn't find " + gameName + " 😓 Please try again!");
                    return Task.CompletedTask;
                }

                // check channel type = voice
                //if (message.

                // check that the channel doesn't already have a game running
                if (activeGames.ContainsKey(message.Channel.Id))
                {
                    message.Channel.SendMessageAsync("Looks like this channel is already running a game! 😓 " +
                        "Please finish the current one, or -play in a different text channel!");
                    return Task.CompletedTask;
                }


                launchGame(newGame, message.Channel);
            }

            return Task.CompletedTask;
        }

        // Creates and starts a new game of type enumName in passed Discord channel.
        public void launchGame(DiscardGames enumName, ISocketMessageChannel channel)
        {
            if (enumName == DiscardGames.NoThanks)
            {
                activeGames.Add(channel.Id, new NoThanksGame());
                channel.SendMessageAsync("Starting a game of No Thanks!", false, activeGames[channel.Id].Blurb());
            } else if (enumName == DiscardGames.IncanGold)
            {
                activeGames.Add(channel.Id, new IncanGoldGame());
                channel.SendMessageAsync("Starting a game of Incan Gold!", false, activeGames[channel.Id].Blurb());
            } else if (enumName == DiscardGames.SixNimmt)
            {
                activeGames.Add(channel.Id, new SixNimmtGame());
                channel.SendMessageAsync("Starting a game of 6 Nimmt!", false, activeGames[channel.Id].Blurb());
            }

        }

        public static string enumToFormalName(DiscardGames enumName) {
            if (enumName == DiscardGames.IncanGold)
            {
                return "Incan Gold";
            } else if (enumName == DiscardGames.NoThanks)
            {
                return "No Thanks!";
            } else if (enumName == DiscardGames.SixNimmt)
            {
                return "6 Nimmt!";
            }

            return "GAMENOTFOUND";
        }

        // make sure you null check! if the game is not found the returned file will be null
        public static string enumToFilePath(DiscardGames enumName)
        {
            if (enumName == DiscardGames.IncanGold)
            {
                return "incangold.jpg";
            }
            else if (enumName == DiscardGames.NoThanks)
            {
                return "nothanks.jpg";
            }
            else if (enumName == DiscardGames.SixNimmt)
            {
                return "sixnimmt.jpg";
            }

            return null;
        }

        public static DiscardGames findGameFromString(string toFind)
        {
            toFind = toFind.Trim().ToLower().Replace(" ", string.Empty);
            Console.WriteLine("toFind looks like:\n" + toFind);
            if (toFind == "nothanks" || toFind == "nothanks!")
            {
                return DiscardGames.NoThanks;
            } else if (toFind == "6nimmt" || toFind == "sixnimmt" || 
                toFind == "sechsnimmt" || toFind == "6nimmt!" || toFind == "sixnimmt!" || toFind == "sechsnimimt!")
            {
                return DiscardGames.SixNimmt;
            } else if (toFind == "incangold" || toFind == "incangold!")
            {
                return DiscardGames.IncanGold;
            }
            return DiscardGames.NOTFOUND;
        }

        private Task MessageHandler(SocketMessage message)
        {
            if (!message.Content.StartsWith('!'))
            {
                return Task.CompletedTask;
            }
            if (message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            return RouteMessage(message.Content.Substring(1).Trim(), message);
        }
    }
}
