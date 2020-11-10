using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.MessageReceived += CommandHandler;
            _client.Log += Log;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            //var token = File.ReadAllText("token.txt");
            var token = File.ReadAllText("token.txt");
            //Console.WriteLine("token is :" + token);

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task CommandHandler(SocketMessage message)
        {
            if (!message.Content.StartsWith('-'))
            {
                return Task.CompletedTask;
            }
            if (message.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            string author = message.Author.Username;
            string contents = message.Content;
            Console.WriteLine($"{author} said:\n{contents}");

            if (message.Content.Substring(1) == "bugsnax")
            {
                message.Channel.SendMessageAsync("it's bugsnax");
                message.AddReactionAsync(new Emoji("\U0001f495"));

                Task<RestUserMessage> sent = message.Channel.SendFileAsync("bugsnax.jpg");

                
                sent.Result.AddReactionsAsync(new IEmote[] { new Emoji("🇧"), new Emoji("🇺"), new Emoji("🇬")});
                Console.WriteLine("Attempted to add new emojis");
            }

            return Task.CompletedTask;
        }
    }
}
