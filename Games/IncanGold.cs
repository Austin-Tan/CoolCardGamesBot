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
    public class IncanGoldGame: ICardGame
    {
        public string Name
        {
            get => "Incan Gold";
        }

        public string ImageURL
        {
            get => "https://intentionalgeek.files.wordpress.com/2013/04/incan-gold-box-med.jpg";
        }

        public ulong ChannelID
        {
            get => _channelID;
        }

        private ulong _channelID;

        public IncanGoldGame(ulong channelID)
        {
            _channelID = channelID;
        }

        public Embed Blurb()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "Incan Gold",
                Description = "***Incan Gold*** (also released as *Diamant*) is a multiplayer card game designed by Alan R. Moon and Bruno Faidutti," +
                "published in 2005 in Germany by Schmidt Spiele."
            };
            builder.WithImageUrl(ImageURL)
                .AddField("Playtime", "Twenty-five minutes.")
                .AddField("Overview", "Players are adventurers hoarding treasure in an ancient temple. On every turn, " + 
                "you choose to either:\n" +
                "1) Descend one card deeper into the temple, sharing any gems earned, but risk"
                + " flipping a hazardous card and losing every gem nabbed this round.\n" +
                "2) Safely exit the temple, banking every gem earned this round but sitting out til the next one.\n" + 
                "The **GOAL of the game** is to amass the most gems over five rounds!");
            return builder.Build();
        }
    }
}
