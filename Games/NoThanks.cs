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
    public class NoThanksGame
    {
        public int MinPlayers
        {
            get => 3;
        }
        public int MaxPlayers
        {
            get => 7;
        }

        public string Name
        {
            get => "No Thanks!";
        }

        public string ImageURL
        {
            get => "https://crystal-cdn3.crystalcommerce.com/photos/5202483/pic2602161_md.jpg";
        }

        public NoThanksGame()
        {
        }

        public Embed Blurb()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "No Thanks!",
                Description = "***No Thanks!*** is a card game for three to seven players designed by Thorsten Gimmler."
            };
            builder.WithImageUrl(ImageURL)
                .AddField("Playtime", "Twenty minutes.")
                .AddField("Overview", "A deck of cards numbered 3 to 35 is shuffled with nine removed at random. " + 
                "You are given a number of chips, each worth negative one point. " +
                "Every turn, you are presented with a card from the deck and have to either:\n" +
                "1) Sacrifice a chip to pass the card to the next player.\n" +
                "2) Take the card and add its value to your total score. You keep any chips on the card for later use.\n" + 
                "The **GOAL of the game** is to have the fewest points, so minimize taking cards and maximize your number of chips!");
            return builder.Build();
        }



        public void StartGame()
        {
            // Add players
            // prepare deck
            // queue or array of players
        }
    }
}
