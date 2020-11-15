using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public abstract class CardGame
    {
        public abstract int MinPlayers
        {
            get;
        }
        public abstract int MaxPlayers
        {
            get;
        }
        public string Name
        {
            get;
        }

        string ImageURL
        {
            get;
        }

        public virtual Embed Blurb()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = $"{Name}",
                Description = $"***{Name}*** is a game for {MinPlayers}-{MaxPlayers} players."
            };
            return builder.WithImageUrl(ImageURL).Build();
        }

        public abstract void StartGame();

    }
}
