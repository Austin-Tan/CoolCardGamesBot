using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Games;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot {
    public enum DiscardGames {
        NoThanks,
        NOTFOUND
    }

    class Program {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        // Dictionary mapping IDs to active channels
        public Dictionary<ulong, ChannelHandler> activeChannels;

        public async Task MainAsync() {

            DiscordSocketConfig config = new DiscordSocketConfig { MessageCacheSize = 64, GuildSubscriptions = false };
            // When Discord.Net.WebSocket receives a stable update to include GatewayIntents, add here
            // and unsub from typing, etc. instead of guildsubscriptions

            _client = new DiscordSocketClient(config);
            _client.MessageReceived += MessageHandler;
            _client.ReactionAdded += ReactionAdder;
            _client.ReactionRemoved += ReactionRemover;
            _client.Log += Log;

            // Copy-Paste your token in Environment Variables under User.
            // Can select System-wide Env Variables instead of User, just change the 2nd Param for targeting.
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN", EnvironmentVariableTarget.User);

            activeChannels = new Dictionary<ulong, ChannelHandler>();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetActivityAsync(new Game("!help", ActivityType.Listening, ActivityProperties.Spectate, "details??"));

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg) {
            return Task.CompletedTask;
        }

        private ChannelHandler GetChannelHandler(ISocketMessageChannel channel) {
            ulong id = channel.Id;
            if (!activeChannels.ContainsKey(id)) {
                activeChannels.Add(id, new ChannelHandler(channel));
            }
            return activeChannels[id];
        }

        private Task ReactionRouter( ISocketMessageChannel channel, SocketReaction reaction, bool adding) {
            // This is a reaction we added ourselves. Ignore it.
            if (reaction.UserId == _client.CurrentUser.Id) {
                return Task.CompletedTask;
            }

            ChannelHandler handler = GetChannelHandler(channel);
            handler.ProcessReaction(reaction.MessageId, reaction.Emote.Name, reaction.UserId, adding);

            return Task.CompletedTask;
        }

        private Task ReactionAdder(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction) {
            return ReactionRouter(channel, reaction, true);
        }

        private Task ReactionRemover(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction) {
            return ReactionRouter(channel, reaction, false);
        }

        private Task MessageHandler(SocketMessage message) {
            if (!message.Content.StartsWith('!')) {
                return Task.CompletedTask;
            }
            if (message.Author.IsBot) {
                return Task.CompletedTask;
            }

            string command = message.Content.Substring(1).ToLower().Trim();
            if (command.StartsWith("test")) {
                // any test things do here

            } else if (command.StartsWith("help")) {
                message.Channel.SendMessageAsync(null, false, HelpMessage());
            } else if (command.StartsWith("bugsnax")) {
                SendBugsnax(message);
            } else {
                ChannelHandler handler = GetChannelHandler(message.Channel);
                handler.ProcessMessage(message, command);
            }

            return Task.CompletedTask;
        }

        private Embed HelpMessage() {
            EmbedBuilder builder = new EmbedBuilder {
                Title = "HELP - Discard Bot",
                Description = "List of commands:",
            };
            builder.WithAuthor(_client.CurrentUser)
                .AddField("!help", "gives you this messsage!")
                .AddField("!play GameName", "WIP. Hopefully starts a game of GameName for you.")
                .AddField("List of games for !play:", "No Thanks!")
                .AddField("!rules", "NOT IMPLEMENTED")
                .AddField("!status", "NOTIMPLEMENTED")
                .WithColor(Color.Orange);
            return builder.Build();
        }

        private void SendBugsnax(SocketMessage message) {
            message.Channel.SendMessageAsync("it's bugsnax, " + message.Author.Mention);
            message.AddReactionAsync(new Emoji("\U0001f495"));
            Task<RestUserMessage> sent = message.Channel.SendFileAsync("bugsnax.jpg");
            sent.Result.AddReactionsAsync(new IEmote[] { new Emoji("🇧"), new Emoji("🇺"), new Emoji("🇬") });
        }

        private Task RouteMessage(string command, SocketMessage message) {
            string author = message.Author.Username;
            Console.WriteLine($"{author} asked:\n{command}");

            if (command.ToLower().Trim() == "test") {
            } else if (command.ToLower().Trim().StartsWith("kill")) {

            } else if (command.Split(' ')[0].ToLower() == "play") {
                // find right game/validate string
                string gameName = command.Substring(command.ToLower().IndexOf("play ") + "play ".Length);
                DiscardGames newGame = findGameFromString(gameName);

                // make sure it's an actually implemented game
                if (newGame == DiscardGames.NOTFOUND) {
                    message.Channel.SendMessageAsync("Sorry, I couldn't find " + gameName + " 😓 Please try again!");
                    return Task.CompletedTask;
                }

                // make sure you're in a group or text channel
                if (message.Channel.GetType() != typeof(SocketTextChannel) && message.Channel.GetType() != typeof(SocketGroupChannel)) {
                    message.Channel.SendMessageAsync("Sorry, !play is not available for channels of type " + message.Channel.GetType());
                    return Task.CompletedTask;
                }


                // check that the channel doesn't already have a game running
                if (activeGames.ContainsKey(message.Channel.Id)) {
                    message.Channel.SendMessageAsync("Looks like this channel is already running a game! 😓 " +
                        "Please finish the current one, or -play in a different text channel!");
                    return Task.CompletedTask;
                }

                launchGame(newGame, message.Channel, message.Author);
            } else {
                if (message.Channel.GetType() == typeof(SocketDMChannel)) {
                    message.Channel.SendMessageAsync("Sorry I didn't seem to understand your message!" +
                        " 😓 Try !help for assistance.");
                }
            }
            return Task.CompletedTask;
        }

        // Creates and starts a new game of type enumName in passed Discord channel.
        public async void launchGame(DiscardGames enumName, ISocketMessageChannel channel, SocketUser author) {
            if (enumName == DiscardGames.NoThanks) {
                activeGames.Add(channel.Id, new GameHandler(new NoThanksGame()));
                await channel.SendMessageAsync("Starting a game of No Thanks!", false, activeGames[channel.Id].activeGame.Blurb());
            } else {
                await channel.SendMessageAsync("this is bad i'm in an else case i shouldn't ever be in.");
                return;
            }

            GameHandler handler = activeGames[channel.Id];

            var reactTo = await channel.SendMessageAsync("React to this message with 👍 to join!\nCurrent Players: ");
            await reactTo.AddReactionAsync(new Emoji("👍"));

            handler.messageMap.Add(reactTo.Id, handler.JoinGameMessage);
        }

        public static DiscardGames findGameFromString(string toFind) {
            toFind = toFind.Trim().ToLower().Replace(" ", string.Empty);
            Console.WriteLine("toFind looks like:\n" + toFind);
            if (toFind == "nothanks" || toFind == "nothanks!") {
                return DiscardGames.NoThanks;
            }
            return DiscardGames.NOTFOUND;
        }
    }
}
