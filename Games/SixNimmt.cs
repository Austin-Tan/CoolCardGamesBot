using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Games
{
    public class SixNimmtGame: ICardGame
    {
        public string Name
        {
            get => "6 Nimmt!";
        }

        public string ImageURL
        {
            get => "https://x.boardgamearena.net/data/themereleases/201107-1937/games/sechsnimmt/200213-1217/img/game_box.png";
        }

        public ulong ChannelID
        {
            get => _channelID;
        }

        private ulong _channelID;

        public SixNimmtGame(ulong channelID)
        {
            _channelID = channelID;
        }

        public Embed Blurb()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "6 Nimmt!",
                Description = "***6 Nimmt!*** (*Sixth Takes!* in German) is a card game for 2–10 players designed by Wolfgang Kramer in 1994 and published by Amigo Spiele."
            };
            builder.WithImageUrl(ImageURL)
                .AddField("Playtime", "Fifteen minutes per round.")
                .AddField("Overview", "A deck of cards numbered 1 to 103 is shuffled, and ten are dealt to each player. " + 
                "Each player will silently choose a card from their hand, and everyone reveals them at the same time. "+
                "The cards are sorted into four rows based on their value, and if any card ends up being the **sixth** " + 
                "card of the row, that player must take the entire row of cards and add each's value to the player's total.\n" +
                "The **GOAL of the game** is to have the fewest points, so minimize taking cards!");
            return builder.Build();
        }
    }
}
