using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    interface ICardGame
    {
        string Name
        {
            get;
        }

        string ImageURL
        {
            get;
        }

        public Embed Blurb();
    }
}
