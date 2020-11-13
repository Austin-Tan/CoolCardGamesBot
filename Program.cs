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
            _client.ReactionAdded += ReactionAdder;
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
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task RouteMessage(string command, SocketMessage message)
        {
            string author = message.Author.Username;
            Console.WriteLine($"{author} asked:\n{command}");

            if (command.ToLower().Trim() == "test")
            {
                string imageURL = "https://www.pojo.com/wp-content/uploads/2018/01/no-thanks-box-art.jpg";
                EmbedBuilder builder = new EmbedBuilder { ImageUrl = $"attachment://{imageURL}" };
                Embed emb = builder.Build();
                message.Channel.SendMessageAsync("msg1");
                message.Channel.SendMessageAsync(null, false, emb);

                //imageURL = "D:/GameDev/DiscordBot/bin/Debug/net5.0/nothanks.jpg";

                builder.WithImageUrl($"{imageURL}");
                message.Channel.SendMessageAsync("msg2");
                message.Channel.SendMessageAsync(null, false, builder.Build());
            } else if (command.ToLower().Trim() == "help")
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
                    .AddField("!bugsnax", "try it for a surprise image! *1% chance of losing all your primogems*")
                    .AddField("List of games for !play:", "No Thanks!, 6 Nimmt!, Incan Gold")
                    .WithColor(Color.Orange);
                Embed msg = builder.Build();

                message.Channel.SendMessageAsync(null, false, msg);
            } else if (command.ToLower().Trim() == "bugsnax")
            {
                message.Channel.SendMessageAsync("it's bugsnax, " + message.Author.Mention);
                message.AddReactionAsync(new Emoji("\U0001f495"));

                Task<RestUserMessage> sent = message.Channel.SendFileAsync("bugsnax.jpg");


                sent.Result.AddReactionsAsync(new IEmote[] { new Emoji("🇧"), new Emoji("🇺"), new Emoji("🇬") });
            } else if (command.Split(' ')[0].ToLower() == "echo")
            {
                string toSend = command.Substring(command.ToLower().IndexOf("echo ") + "echo ".Length);
                message.Channel.SendMessageAsync(toSend + "\n- " + message.Author.Mention);
            } else if (command.ToLower().Trim().StartsWith("kill")) {
                if (activeGames.ContainsKey(message.Channel.Id))
                {
                    Task<RestUserMessage> sent = message.Channel.SendMessageAsync("Do you really want to kill this game?\n" +
                        "🗑️ to confirm deletion. 👎 to keep playing.");
                    sent.Result.AddReactionsAsync(new IEmote[] { new Emoji("👎"), new Emoji("🗑️") });
                } else
                {
                    message.Channel.SendMessageAsync("Could not find a game running in this channel to kill!");
                }
            } else if (command.Split(' ')[0].ToLower() == "play")
            {
                // find right game/validate string
                string gameName = command.Substring(command.ToLower().IndexOf("play ") + "play ".Length);
                DiscardGames newGame = findGameFromString(gameName);

                // make sure it's an actually implemented game
                if (newGame == DiscardGames.NOTFOUND)
                {
                    message.Channel.SendMessageAsync("Sorry, I couldn't find " + gameName + " 😓 Please try again!");
                    return Task.CompletedTask;
                }

                // make sure you're in a group or text channel
                if (message.Channel.GetType() != typeof(SocketTextChannel) && message.Channel.GetType() != typeof(SocketGroupChannel)) {
                    message.Channel.SendMessageAsync("Sorry, !play is not available for channels of type " + message.Channel.GetType());
                    return Task.CompletedTask;
                }


                // check that the channel doesn't already have a game running
                if (activeGames.ContainsKey(message.Channel.Id))
                {
                    message.Channel.SendMessageAsync("Looks like this channel is already running a game! 😓 " +
                        "Please finish the current one, or -play in a different text channel!");
                    return Task.CompletedTask;
                }


                launchGame(newGame, message.Channel);
            } else
            {
                if (message.Channel.GetType() == typeof(SocketDMChannel))
                {
                    message.Channel.SendMessageAsync("Sorry I didn't seem to understand your message!" +
                        " 😓 Try !help for assistance.");
                }
            }

            return Task.CompletedTask;
        }

        // Creates and starts a new game of type enumName in passed Discord channel.
        public void launchGame(DiscardGames enumName, ISocketMessageChannel channel)
        {
            if (enumName == DiscardGames.NoThanks)
            {
                activeGames.Add(channel.Id, new NoThanksGame(channel.Id));
                channel.SendMessageAsync("Starting a game of No Thanks!", false, activeGames[channel.Id].Blurb());
            } else if (enumName == DiscardGames.IncanGold)
            {
                activeGames.Add(channel.Id, new IncanGoldGame(channel.Id));
                channel.SendMessageAsync("Starting a game of Incan Gold!", false, activeGames[channel.Id].Blurb());
            } else if (enumName == DiscardGames.SixNimmt)
            {
                activeGames.Add(channel.Id, new SixNimmtGame(channel.Id));
                channel.SendMessageAsync("Starting a game of 6 Nimmt!", false, activeGames[channel.Id].Blurb());
            }

        }

        //public static string enumToFormalName(DiscardGames enumName) {
        //    if (enumName == DiscardGames.IncanGold)
        //    {
        //        return "Incan Gold";
        //    } else if (enumName == DiscardGames.NoThanks)
        //    {
        //        return "No Thanks!";
        //    } else if (enumName == DiscardGames.SixNimmt)
        //    {
        //        return "6 Nimmt!";
        //    }

        //    return "GAMENOTFOUND";
        //}

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

        private Task ReactionAdder(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            // This is a reaction we added ourselves. Ignore it.
            if (reaction.UserId == _client.CurrentUser.Id) {
                Console.WriteLine("We reacted just now.");
                return Task.CompletedTask;
            }

            Console.WriteLine("Looking for message just reacted");
            if (reaction.Message.IsSpecified)
            {
                Console.WriteLine("Message is specified");
                SocketUserMessage msg = reaction.Message.Value;
                Console.WriteLine("message: " + msg.Content);
                
                // super clunky to hardcode like this, set up a state for the game
                if (msg.Author.Id == _client.CurrentUser.Id && msg.Content == "Do you really want to kill this game?\n" +
                        "🗑️ to confirm deletion. 👎 to keep playing.")
                {
                    Console.WriteLine("On a kill message");
                    if (reaction.Emote.Name == new Emoji("🗑️").Name || reaction.Emote.Name == new Emoji("🗑").Name)
                    {
                        channel.SendMessageAsync($"Game of ***{activeGames[channel.Id].Name}*** deleted 🗑️");
                        activeGames.Remove(channel.Id);
                    } else
                    {
                        channel.SendMessageAsync("We'll keep playing ***{activeGames[channel.Id].Name}*** 😃");
                        channel.DeleteMessageAsync(msg.Id);
                    }
                    channel.DeleteMessageAsync(msg.Id);
                }
            }
            
            
            return Task.CompletedTask;
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
