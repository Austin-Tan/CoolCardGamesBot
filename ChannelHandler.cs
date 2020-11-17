using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    class ChannelHandler
    {
        // We could choose to go to a higher interface like ISocketMessageChannel
        // but I think we always want this more specific scope.
        SocketTextChannel channel;

        // the keys are the ulongs - messageIds of relevant messages
        private Dictionary<ulong, int> messages;

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
            messages = new Dictionary<ulong, int>();
        }


        public void ProcessReaction(ulong messageId, string emote, ulong userId, bool adding) {

        }


        // super clunky to hardcode like this, set up a state for the game
        //if (msg.Author.Id == _client.CurrentUser.Id && msg.Content == "Do you really want to kill this game?\n" +
        //        "🗑️ to confirm deletion. 👎 to keep playing.") {
        //    Console.WriteLine("On a kill message");
        //    if (reaction.Emote.Name == new Emoji("🗑️").Name || reaction.Emote.Name == new Emoji("🗑").Name) {
        //        channel.SendMessageAsync($"Game of ***{activeGames[channel.Id].activeGame.Name}*** deleted 🗑️");
        //        activeGames.Remove(channel.Id);
        //    } else {
        //        channel.SendMessageAsync("We'll keep playing ***{activeGames[channel.Id].Name}*** 😃");
        //        channel.DeleteMessageAsync(msg.Id);
        //    }
        //    channel.DeleteMessageAsync(msg.Id);
        //} else {
        //    Console.WriteLine("Hitting the map now with react by " + reaction.User.Value.Username);
        //    activeGames[channel.Id].messageMap[msg.Id](msg, reaction.User.Value);
        //}
    }
}
