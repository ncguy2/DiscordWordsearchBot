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

            // webInterface = new WebInterface(core);
            // webInterface.Initialise();

            new Program().MainAsync(core).GetAwaiter().GetResult();
        }

        public async Task MainAsync(Core core) {
            await core.Initialise();
            await Task.Delay(-1);
        }
    }
}