using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class GameHandler
    {
        public static string joinMessage = "React to this message with 👍 to join!\nCurrent Players: ";

        public Dictionary<ulong, Func<IUserMessage, IUser, bool>> messageMap = new Dictionary<ulong, Func<IUserMessage, IUser, bool>>();

        public CardGame activeGame;

        // each dictionary entry is a player, with their ID and username as key/val respectively
        public Dictionary<ulong, string> Players = new Dictionary<ulong, string>();

        // 
        public bool JoinGameMessage(IUserMessage message, IUser player)
        {
            if (!message.Reactions.ContainsKey(new Emoji("👍")))
            {
                Console.WriteLine("Not good! There was no thumbs emoji on this joingamemessge");
                return false;
            }
            var users = message.GetReactionUsersAsync(new Emoji("👍"), activeGame.MaxPlayers);
            var users2 = AsyncEnumerableExtensions.FlattenAsync<IUser>(users).Result;
            bool foundUser = false;
            foreach (var user in users2)
            {
                if (user.Id == player.Id)
                {
                    Console.WriteLine(player.Username + " was found in users2");
                    AddPlayer(player);
                    foundUser = true;
                    break;
                }
            }
            if (!foundUser)
            {
                Console.WriteLine(player.Username + " was NOT found in users2");
                RemovePlayer(player);
            }
            message.ModifyAsync(x => x.Content = joinMessage + string.Join(", ", Players.Values));

            return true;
        }

        public GameHandler(CardGame gameToPlay)
        {
            activeGame = gameToPlay;

        }

        public void AddPlayer(IUser player)
        {
            if (Players.ContainsKey(player.Id))
            {
                Console.WriteLine("Uh oh! Player " + player.Username + " is already in the game.");
                return;
            }
            Players.Add(player.Id, player.Username);
        }

        public void RemovePlayer(IUser player)
        {
            if (!Players.ContainsKey(player.Id))
            {
                Console.WriteLine("Bad! We tried to remove " + player.Username + " but they weren't even playing!");
            }
            Players.Remove(player.Id);
        }
    }
}
