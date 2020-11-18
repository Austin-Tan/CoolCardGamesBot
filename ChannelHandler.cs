using Discord;
using Discord.WebSocket;
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
            status = ChannelStatus.Listening;
        }



        // not yet implemented to kill the game
        private void EndGame() {
            status = ChannelStatus.Listening;
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

        public void ProcessReaction(ulong messageId, string emote, ulong userId, bool adding) {
            if (messages.ContainsKey(messageId)) {
                messages[messageId](messageId, emote, userId, adding);
            }
        }

        // string command has already been trimmed and lower-cased
        public void ProcessMessage(SocketMessage message, string command) {
            if (command.StartsWith("kill")) {
                _ = SendKillMessage();
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
                // kill the lobby
            }
        }

        private string GetUsername(ulong userId) {
            if (players.ContainsKey(userId)) {
                return players[userId].Username;
            }
            return channel.GetUser(userId).Username;
        }
    }
}
