using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public enum ChannelStatus {
        Listening,
        WaitingLobby,
        Playing,
        KillMessage
    }

    class ChannelHandler
    {
        // We could choose to go to a higher interface like ISocketMessageChannel
        // but I think we always want this more specific scope.
        SocketTextChannel channel;

        public ChannelStatus status;

        // the keys are the messageIds - delegates for handling reactions
        private Dictionary<ulong, Action<ulong, string, ulong, bool>> messages;

        // discord userIds and Users playing the game!
        private Dictionary<ulong, IUser> players;

        // these two are just used for the lobby
        private LinkedList<string> orderedPlayers;
        private RestUserMessage lobbyRestMessage; // only used for !kill, super clunky

        private NoThanksGame runningGame;

        public ChannelHandler(ISocketMessageChannel newChannel)
        {
            var castedChannel = newChannel as SocketTextChannel;
            if (castedChannel == null)
            {
                throw new InvalidCastException(
                    "Could not cast passed channel " + 
                    "to SocketTextChannel");
            }
            channel = castedChannel;
            messages = new Dictionary<ulong, Action<ulong, string, ulong, bool>>();
            players = new Dictionary<ulong, IUser>();
            orderedPlayers = new LinkedList<string>();
            status = ChannelStatus.Listening;
        }


        // not yet implemented to kill the game
        private void EndGame() {
            status = ChannelStatus.Listening;
            runningGame = null;
        }

        public void ProcessReaction(ulong messageId, string emote, ulong userId, bool adding) {
            if (messages.ContainsKey(messageId)) {
                messages[messageId](messageId, emote, userId, adding);
            }
        }

        // string command has already been trimmed and lower-cased
        public void ProcessMessage(SocketMessage message, string command) {
            if (command.StartsWith("kill")) {
                _ = SendKillMessage();
            } else if (command.StartsWith("play")) {
                _ = PlayGame(message, command.Substring(command.IndexOf("play") + "play".Length).Trim());
            } else {
                _ = channel.SendMessageAsync(
                    "Sorry, !" + command.Split(' ')[0] + " has not been" +
                    "implemented 😓 Try !help for a list of commands.");
            }
        }

        private async Task PlayGame(SocketMessage message, string gameToFind) {
            if (status != ChannelStatus.Listening) {
                await channel.SendMessageAsync(
                    "Please end the current game or lobby before " +
                    "starting a new one!");
                return;
            }

            DiscardGames game = Program.FindGameFromString(gameToFind);
            if (game == DiscardGames.NOTFOUND) {
                await channel.SendMessageAsync(
                    "Sorry, I couldn't find " + gameToFind + " 😓 Please try" +
                    "again or run !help for a list of games you can !play.");
            } else {
                // hard coding no thanks
                _ = StartLobby(game);
            }
        }

        private static string JoinGameMessage = "React to this message with 👍 to join! Current Players:\n";

        // make sure you don't pass DiscardGames.NOTFOUND to this method
        private async Task StartLobby(DiscardGames targetGame) {
            if (targetGame == DiscardGames.NoThanks) {
                runningGame = new NoThanksGame();
            }
            status = ChannelStatus.WaitingLobby;

            await channel.SendMessageAsync("Starting a game of " + runningGame.Name, false, runningGame.Blurb());
            var reactTo = await channel.SendMessageAsync(JoinGameMessage);
            await reactTo.AddReactionAsync(new Emoji("👍"));
            messages.Add(reactTo.Id, ReactJoinMessage);
            lobbyRestMessage = reactTo;
        }

        private Task<IMessage> GetMessage(ulong messageId) {
            var castedChannel = channel as IMessageChannel;
            if (castedChannel == null) {
                throw new InvalidCastException("Couldn't cast SocketTextChannel to IMessageChannel");
            }
            return castedChannel.GetMessageAsync(messageId, CacheMode.AllowDownload);
        }


        private async void ReactJoinMessage(ulong messageId, string emote, ulong userId, bool adding) {
            if (emote == "👍") {
                if (adding) {
                    players.Add(userId, await GetUser(userId));
                    Console.WriteLine("is this null? : " + channel.GetUser(userId));
                    Console.WriteLine("WHat about this? : " + players[userId]);
                    orderedPlayers.AddLast(GetUsername(userId));
                } else {
                    players.Remove(userId);
                    orderedPlayers.Remove(GetUsername(userId));
                }
                UpdateLobbyMessage();
            
            /// *** STARTS THE GAME ***
            } else if (emote == "✅" && adding) {
                await channel.DeleteMessageAsync(messageId);
                messages.Remove(messageId);
                 await channel.SendMessageAsync(
                    "Starting a game of " + runningGame.Name + " with " +
                    string.Join<string>(", ", orderedPlayers));
                status = ChannelStatus.Playing;
            }
        }

        private async void UpdateLobbyMessage() {
            if (lobbyRestMessage != null) {
                string playerString = string.Join<string>(", ", orderedPlayers);
                int count = players.Count;
                playerString += $"\nWe have {count} players.";
                if (count >= runningGame.MinPlayers && count <= runningGame.MaxPlayers) {
                    playerString += $" {orderedPlayers.First}, react with ✅ to begin!";
                    await lobbyRestMessage.AddReactionAsync(new Emoji("✅"));
                }
                await lobbyRestMessage.ModifyAsync(x => 
                    x.Content = JoinGameMessage + playerString);
            } else {
                Console.WriteLine("Error! couldn't cast or find Lobby message");
            }
        }

        private async Task SendKillMessage() {
            if (status == ChannelStatus.Playing) {
                var sentMsg = await channel.SendMessageAsync(
                    "Do you really want to kill this game?\n" +
                    "👎 to keep playing. 🗑️ to confirm deletion.");
                await sentMsg.AddReactionsAsync(new IEmote[] { new Emoji("👎"), new Emoji("🗑️") });
                messages.Add(sentMsg.Id, ReactKillMessage);
            } else if (status == ChannelStatus.KillMessage) {
                await channel.SendMessageAsync("We are already trying to kill the game, please react above.");
            } else if (status == ChannelStatus.WaitingLobby) {
                await lobbyRestMessage.ModifyAsync(x => x.Content = "This lobby has been canceled by the command !kill.");
                await channel.SendMessageAsync("The lobby for " + runningGame.Name + " has been canceled.");
                runningGame = null;
                status = ChannelStatus.Listening;
            } else if (status == ChannelStatus.Listening) {
                await channel.SendMessageAsync("No active game found to kill.");
            }
        }

        // this method is synchronous. we may have some issues there.
        private void ReactKillMessage(ulong messageId, string emote, ulong userId, bool adding) {
            if (adding && (emote == "👎" || emote == "🗑️")) {
                if (emote == "🗑️") {
                    channel.SendMessageAsync(GetUsername(userId) + " has ended the game!");
                    EndGame();
                } else {
                    channel.SendMessageAsync(GetUsername(userId) + " has resumed, we'll keep playing!");
                }
                channel.DeleteMessageAsync(messageId);
                messages.Remove(messageId);
            }
        }

        private async Task<IUser> GetUser(ulong userId) {
            var castChannel = channel as IGuildChannel;
            if (castChannel != null) {
                var user = await castChannel.GetUserAsync(userId, CacheMode.AllowDownload);
                var castUser = user as IUser;
                if (castUser != null) {
                    return castUser;
                }
            }
            throw new InvalidCastException("Can't cast channel to guildchannel");
        }

        private string GetUsername(ulong userId) {
            if (players.ContainsKey(userId)) {
                return players[userId].Username;
            }
            return channel.GetUser(userId).Username;
        }
    }
}
