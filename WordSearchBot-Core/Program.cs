using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WordSearchBot.Core.Data;
using WordSearchBot.Core.Data.Facade;
using WordSearchBot.Core.Model;
using WordSearchBot.Core.Web;

namespace WordSearchBot.Core {
    internal class Program {
        private static WebInterface webInterface;

        public static void PrepMappers() {
            // BsonMapper.Global.IncludeFields = true;
            // Suggestion.Register(BsonMapper.Global);
        }

        private static void Main(string[] args) {
            // PrepMappers();
            //
            Core core = new();

            // try {
            //     webInterface = new WebInterface(core);
            //     webInterface.Initialise();
            // } catch (HttpListenerException hle) {
            //     Console.WriteLine(hle.ToString());
            //     Console.WriteLine("Unable to bind to requested port, moving web interface to port 8080");
            //     webInterface = new WebInterface(core, port: 8080);
            //     webInterface.Initialise();
            // } catch (Exception e) {
            //     Console.WriteLine(e.ToString());
            // }

            new Program().MainAsync(core).GetAwaiter().GetResult();
        }

        public async Task MainAsync(Core core) {
            await core.Initialise();
            await Task.Delay(-1);
        }
    }
}