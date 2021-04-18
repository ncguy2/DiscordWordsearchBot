using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace WordSearchBot.Core {
    class Program {
        static void Main(string[] args) {
            new Program().MainAsync().GetAwaiter().GetResult();
        }


        public async Task MainAsync() {
            Core core = new();
            await core.Initialise();
            await Task.Delay(-1);
        }
    }
}