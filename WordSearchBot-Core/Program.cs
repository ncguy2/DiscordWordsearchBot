using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace WordSearchBot.Core {
    class Program {
        static void Main(string[] args) {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private DiscordSocketClient client;
        private readonly string Token = GlobalConfig.GetValue(ConfigKeys.TOKEN).GetString(false, false);

        public async Task MainAsync() {
            client = new DiscordSocketClient();
            client.Log += Log;

            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg) {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

    }
}