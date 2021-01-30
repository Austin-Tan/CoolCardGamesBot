using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Rest;

namespace DiscordBot.Games {

    public class IncanGoldGame : GameInterface {
        public int MinPlayers {
            get => 2;
        }
        public int MaxPlayers {
            get => 8;
        }

        public string Name {
            get => "Incan Gold";
        }

        public string ImageURL {
            get => "https://images-na.ssl-images-amazon.com/images/I/71tVfX5NoBL._AC_SL1166_.jpg";
        }

        public IncanGoldGame(ChannelHandler handler) {
            parentChannel = handler;
        }

        public Embed Blurb() {
            EmbedBuilder builder = new EmbedBuilder {
                Title = "Incan Gold",
                Description = "***Incan Gold*** (also released as *Diamant*) is a game for 2-8 players designed by Alan R. Moon and Bruno Faidutti," +
                "published in 2005 in Germany by Schmidt Spiele."
            };
            builder.WithImageUrl(ImageURL)
                .AddField("Playtime", "Twenty minutes.")
                .AddField("Overview", "Players are adventurers hoarding treasure in an ancient temple. On every turn, " +
                "you choose to either:\n" +
                "1) Descend one card deeper into the temple, sharing any gems earned, but risk"
                + " flipping a hazardous card and losing every gem nabbed this round.\n" +
                "2) Safely exit the temple, banking every gem earned this round but sitting out til the next one.\n" +
                "The **GOAL of the game** is to amass the most gems over five rounds!");
            return builder.Build();
        }

        private ChannelHandler parentChannel;
        private Player[] players;
        private int numPlayers;
        private List<Card> deck;
        private List<Card> revealedCards;
        private int round = 0;
        private Player[] remainingPlayers;
        private HashSet<HazardType> trippedHazards;


        private RestUserMessage roundMessage;
        private RestUserMessage reactMessage;



        public void StartGame() {
            // Add Players
            numPlayers = parentChannel.players.Count;
            players = new Player[numPlayers];
            KeyValuePair<ulong, IUser>[] importPlayers = parentChannel.players.ToArray();
            idToPlayer = new Dictionary<ulong, Player>();
            for (int i = 0; i < numPlayers; i ++) {
                players[i] = new Player(
                    importPlayers[i].Value.Username,
                    importPlayers[i].Key);
                idToPlayer.Add(players[i].id, players[i]);
            }
            

            // prepare deck
            StartingDeck();

            NewRound();            
        }


        public async void NewRound() {
            if (round != 5) {
                if (round != 0) {
                    foreach (Card card in revealedCards) {
                        if (card.type == CardType.Hazard) {
                            deck.Add(card);
                        } else if (card.type == CardType.Treasure) {
                            card.remainder = 0;
                            deck.Add(card);
                        }
                    }
                }
                round++;

                deck.Add(new Card(CardType.Relic));
                ShuffleDeck();

                foreach (Player player in players) {
                    player.heldGems = 0;
                }
                remainingPlayers = players;
                trippedHazards = new HashSet<HazardType>();
                revealedCards = new List<Card>();

                roundMessage = await parentChannel.channel.SendMessageAsync(null, false, PrettyPrint());
                DrawCard(true);
            } else {
                EndGame();
            }
        }

        public Embed PrettyPrint() {
            EmbedBuilder builder = new EmbedBuilder {
                Title = "Round " + round
            };

            remainingPlayers.Select<Player, string>(o => o.username);

            string explorers = remainingPlayers[0].username + ((remainingPlayers[0].heldGems == 0) ? "" : $" ({remainingPlayers[0].heldGems})");
            for (int i = 1; i < remainingPlayers.Length; i++) {
                explorers += $"\n{remainingPlayers[i].username}";
                if (remainingPlayers[i].heldGems > 0) {
                    explorers += $" ({remainingPlayers[i].heldGems})";
                }
            }

            string revealed = "🚪";
            for (int i = 0; i < revealedCards.Count(); i++ ) {
                revealed += "  ---  " + PrintCard(revealedCards[i]);
            }

            builder.AddField("Explorers still in the temple", explorers)
                .AddField("Revealed cards and leftover gems", revealed);
            return builder.Build();
        }

        private string PrintCard(Card card, bool drawn = false) {
            switch (card.type) {
                case CardType.Treasure:
                    int value = drawn ? card.value : card.remainder;
                    return $"[{value}💎]";
                case CardType.Hazard:
                    if (drawn) {
                        return $"[{card.hazardType} {hazardEmoji(card.hazardType)}]";
                    } else {
                        return $"[{hazardEmoji(card.hazardType)}]";
                    }
                case CardType.Relic:
                    if (drawn) {
                        return $"[RELIC 💎🌞💎]";
                    } else {
                        return $"[💎🌞💎]";
                    }
            }
            return "";
        }

        private string hazardEmoji(HazardType type) {
            if (type == HazardType.Chungus) {
                return "🐰";
            } else if (type == HazardType.Mummy) {
                return "🧟";
            } else if (type == HazardType.Spiders) {
                return "🕸️";
            } else if (type == HazardType.Snakes) {
                return "🐍";
            } else if (type == HazardType.Quake) {
                return "💥";
            }
            return "didn't find right hazard this is broken you should not see this message";
        }

        private Dictionary<ulong, Player> idToPlayer;
        private Dictionary<ulong, bool> idToAction;

        public async void DrawCard(bool roundStart = false) {
            Card drawn = deck[0];
            deck.RemoveAt(0);
            bool killed = false;
            switch (drawn.type) {
                case CardType.Treasure:
                    drawn.remainder = drawn.value % remainingPlayers.Count();
                    foreach (Player player in remainingPlayers) {
                        player.heldGems += drawn.value / remainingPlayers.Count();
                    }
                    break;
                case CardType.Hazard:
                    if (trippedHazards.Contains(drawn.hazardType)) {
                        await parentChannel.channel.SendMessageAsync("Uh oh fucky wucky we hit two " + hazardEmoji(drawn.hazardType));
                        killed = true;
                    } else {
                        trippedHazards.Add(drawn.hazardType);
                    }
                    break;
                case CardType.Relic:
                    break;
            }
            revealedCards.Add(drawn);

            string drawMessage = "Just drawn: " + PrintCard(drawn, true);

            if (killed) {
                await reactMessage.ModifyAsync(m => m.Content = drawMessage +
                    "\n" + string.Join<string>(", ", remainingPlayers.Select<Player, string>(o => o.username)) +
                    " all lose their banked gems!");
                parentChannel.messages.Remove(reactMessage.Id);
                revealedCards.RemoveAt(revealedCards.Count - 1);
                NewRound();
            } else {
                if (roundStart) {
                    reactMessage = await parentChannel.channel.SendMessageAsync(drawMessage +
                    "\n⛺ to go home and bank your gems. 🤠 to continue delving.");
                    parentChannel.messages.Add(reactMessage.Id, DelveMessage);
                } else {
                    await reactMessage.ModifyAsync(m =>
                    m.Content = drawMessage +
                    "\n⛺ to go home and bank your gems. 🤠 to continue delving.");
                }
                idToAction = new Dictionary<ulong, bool>();
                
                await roundMessage.ModifyAsync(m => m.Embed = PrettyPrint());
                await reactMessage.RemoveAllReactionsAsync();
                await reactMessage.AddReactionsAsync(new IEmote[] { new Emoji("⛺"), new Emoji("🤠") });
            }
        }

        private void EndTurn() {
            List<Player> newRemaining = new List<Player>();
            List<Player> goingHome = new List<Player>();
            foreach(Player player in remainingPlayers) {
                if (idToAction.ContainsKey(player.id) && idToAction[player.id]) {
                    // player remains!
                    newRemaining.Add(player);
                } else {
                    player.bankedGems += player.heldGems;
                    player.heldGems = 0;
                    goingHome.Add(player);
                }
            }
            foreach (Card card in revealedCards) {
                if (goingHome.Count > 0) {
                    if (card.type == CardType.Treasure) {
                        foreach (Player player in goingHome) {
                            player.bankedGems += card.remainder / goingHome.Count();
                        }
                        card.remainder = card.remainder % goingHome.Count();
                    } else if (card.type == CardType.Relic) {
                        if (goingHome.Count() == 1) {
                            card.type = CardType.Treasure;
                            card.value = 0;
                            card.remainder = 0;
                            goingHome.First().bankedGems += relicValue();
                        }
                    }
                }
            }

            remainingPlayers = newRemaining.ToArray();
            if (remainingPlayers.Length == 0) {
                NewRound();
            } else {
                DrawCard();
            }
        }

        public int relicsGotten = 0;
        public int relicValue() {
            relicsGotten++;
            return (relicsGotten < 4) ? 5 : 10;
        }

        bool secretTimerOn = false;
        bool realTimerOn = false;

        private void BackgroundLoop() {
            int realCounter = 10;
            while (true) {
                Thread.Sleep(1000);
                realCounter--;
                if (realTimerOn) {
                    if (realCounter > 3) {
                        realCounter = 3;
                    }
                }

                if (realCounter > 0 && realCounter <= 5) {
                    reactMessage.ModifyAsync(m => m.Content += $" {realCounter}...");
                } else if (realCounter == 0) {
                    EndTurn();
                    break;
                }
            }
            realTimerOn = false;
            secretTimerOn = false;
        }

        private void DelveMessage(ulong messageId, string emote, ulong userId, bool adding) {
            if (idToPlayer.ContainsKey(userId) && remainingPlayers.Contains<Player>(idToPlayer[userId])) {
                if (emote == "⛺" || emote == "🤠") {
                    if (adding) {
                        if (idToAction.Remove(userId)) {
                            reactMessage.RemoveReactionAsync(new Emoji((emote == "🤠") ? "⛺": "🤠"), userId);
                        }
                        idToAction.Add(userId, emote == "🤠");
                        if (remainingPlayers.Count() == 1) {
                            EndTurn();
                        } else if (idToAction.Count == remainingPlayers.Count()) {
                            realTimerOn = true;
                        } else if (!secretTimerOn) {
                            secretTimerOn = true;
                            Task bar = new Task(BackgroundLoop);
                            bar.Start();
                        }
                    } else {
                        idToAction.Remove(userId);
                    }
                }
            }
        }

        public readonly int[] gemsDistribution = { 1, 2, 3, 4, 5, 5, 7, 7, 9, 11, 11, 13, 14, 15, 17 };

        public void StartingDeck() {
            deck = new List<Card>();
            for (int i = 0; i < 3; i ++) {
                deck.Add(new Card(CardType.Hazard, HazardType.Snakes));
                deck.Add(new Card(CardType.Hazard, HazardType.Quake));
                deck.Add(new Card(CardType.Hazard, HazardType.Mummy));
                deck.Add(new Card(CardType.Hazard, HazardType.Spiders));
                deck.Add(new Card(CardType.Hazard, HazardType.Chungus));
            }
            foreach (int value in gemsDistribution) {
                deck.Add(new Card(CardType.Treasure, value));
            }
            ShuffleDeck();   
        }
        
        private Random random = new Random();

        public void ShuffleDeck() {
            int n = deck.Count;
            while (n > 1) {
                n--;
                int k = random.Next(n + 1);
                Card value = deck[k];
                deck[k] = deck[n];
                deck[n] = value;
            }
        }
        public void EndGame() {
            foreach (Player player in players) {
                player.bankedGems += player.heldGems;
                player.heldGems = 0;
            }
            IEnumerable<Player> query = players.OrderBy(player => -1 * player.bankedGems);
            EmbedBuilder bd = new EmbedBuilder();
            bd.Title = $"Game Over - {query.First().username} wins!";
            string scoreboard = "";
            for (int i = 1; i <= query.Count(); i++) {
                scoreboard += $"\n{i}. {query.ElementAt(i - 1).username}: **{query.ElementAt(i - 1).bankedGems}** gems!";
            }
            bd.AddField("Scoreboard", scoreboard);
            parentChannel.channel.SendMessageAsync(null, false, bd.Build());
            parentChannel.EndGame(true);
        }


        public enum CardType {
            Hazard,
            Treasure,
            Relic
        }

        public enum HazardType {
            Snakes,
            Quake,
            Mummy,
            Spiders,
            Chungus
        }

        private class Player {
            // discord name
            public string username;

            // discord unique ID
            public ulong id;

            public bool inTemple;
            public int bankedGems;
            public int heldGems;

            public Player(string username, ulong id) {
                this.username = username;
                this.id = id;

                inTemple = false;
                bankedGems = 0;
                heldGems = 0;
            }

            public override string ToString() {
                return $"{username}: {heldGems} {bankedGems}";
            }
        }

        private class Card {
            public CardType type;
            public int value;
            public int remainder;
            public HazardType hazardType;

            // only used with relics pretty much
            public Card(CardType type) {
                this.type = type;
            }

            // ctor for treasure cards
            public Card(CardType type, int value) {
                this.type = type;
                this.value = value;
            }

            // ctor for hazard cards
            public Card(CardType type, HazardType hazardType) {
                this.type = type;
                this.hazardType = hazardType;
            }
        }
    }
}
