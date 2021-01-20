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
        IncanGold,
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

        private Task Log(LogMessage message) {
            Console.WriteLine($"[General/{message.Severity}] {message}");
            return Task.CompletedTask;
        }


        private ChannelHandler GetChannelHandler(ISocketMessageChannel channel) {
            // check that we're in a server text channel
            if (channel.GetType() != typeof(SocketTextChannel)) {
                channel.SendMessageAsync(
                    "Sorry, **Discard Games Bot** is not available for" +
                    "channels of type " + channel.GetType());
                return null;
            }

            ulong id = channel.Id;
            if (!activeChannels.ContainsKey(id)) {
                activeChannels.Add(id, new ChannelHandler(channel));
            }
            return activeChannels[id];
        }

        private Task ReactionRouter(ISocketMessageChannel channel, SocketReaction reaction, bool adding) {
            // this is a reaction we added ourselves. Ignore it.
            if (reaction.UserId == _client.CurrentUser.Id) {
                return Task.CompletedTask;
            }

            ChannelHandler handler = GetChannelHandler(channel);
            if (handler != null) {
                handler.ProcessReaction(reaction.MessageId, reaction.Emote.Name, reaction.UserId, adding);
            }

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
                if (handler != null) {
                    handler.ProcessMessage(message, command);
                }
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
                .AddField("List of games for !play:", "***No Thanks!***, ***Incan Gold***")
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

        public static DiscardGames FindGameFromString(string toFind) {
            toFind = toFind.Replace(" ", string.Empty);
            if (toFind == "nothanks" || toFind == "nothanks!") {
                return DiscardGames.NoThanks;
            } else if (toFind == "incangold") {
                return DiscardGames.IncanGold;
            }
            return DiscardGames.NOTFOUND;
        }
    }
}
