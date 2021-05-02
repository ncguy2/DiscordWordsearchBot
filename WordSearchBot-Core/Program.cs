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
using LiteDB;
using WordSearchBot.Core.Model;

namespace WordSearchBot.Core {
    class Program {
        public static void PrepMappers() {
            BsonMapper.Global.IncludeFields = true;
            Suggestion.Register(BsonMapper.Global);
        }

        static void Main(string[] args) {
            PrepMappers();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync() {
            Core core = new();
            await core.Initialise();
            await Task.Delay(-1);
        }
    }
}