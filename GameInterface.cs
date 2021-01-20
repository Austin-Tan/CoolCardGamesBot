using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot {
    interface GameInterface {
        public int MinPlayers {
            get;
        }

        public int MaxPlayers {
            get;
        }

        public string Name {
            get;
        }

        public string ImageURL {
            get;
        }

        public Embed Blurb();

        public void StartGame();
    }
}
