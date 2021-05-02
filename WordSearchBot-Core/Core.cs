using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LiteDB;
using WordSearchBot.Core.Model;

namespace WordSearchBot.Core {
    public class Core {

        private DiscordSocketClient client;
        private readonly string Token = GlobalConfig.GetValue(ConfigKeys.TOKEN).GetString(false, false);
        public static readonly ulong BOT_TESTING_CHANNEL_ID = ulong.Parse(GlobalConfig.GetValue(ConfigKeys.TEST_CHANNEL_ID).Value);
        public static readonly ulong BOT_LOG_CHANNEL_ID = ulong.Parse(GlobalConfig.GetValue(ConfigKeys.LOG_CHANNEL_ID).Value);
        public static readonly ulong GUILD_ID = ConfigKeys.GUILD_ID.Get();
        public static readonly string CACHE_DIR_ROOT = GlobalConfig.GetValue(ConfigKeys.Cache.CACHE_DIR).GetString(false, false);


        private DiscordContext Context;
        protected List<Module> Modules = new ();

        [Flags]
        public enum LogLevel {
            FATAL,
            ERROR,
            INFO,
            DEBUG
        }

        private LogLevel loggingLevel = LogLevel.ERROR | LogLevel.INFO | LogLevel.DEBUG;

        private readonly Color[] colours = {
            Color.Red,
            Color.Orange,
            Color.Blue,
            Color.Green
        };

        public T AddModule<T>() where T : Module, new() {
            T mod = new T();
            AddModule(mod);
            return mod;
        }

        public void AddModule(Module mod) {
            Modules.Add(mod);
            mod.AssignCore(this);
            mod.RegisterEvents(GetContext());
        }

        protected DiscordContext GetContext() {
            if (Context != null)
                return Context;

            return Context = new DiscordContext {
                Client = client,
                MessageListener = new EventListener<IUserMessage>(),
                Guild = client.GetGuild(GUILD_ID)
            };
        }

        public async Task Log(LogLevel level, string message, string author = null) {

            string authorTag = "";
            if (author != null)
                authorTag = $"[{author}]";
            Console.WriteLine($"[{Enum.GetName(level)}]{authorTag} {message}");

            if (level != LogLevel.FATAL && (loggingLevel & level) == 0)
                return;

            SocketTextChannel channel = client.GetChannel(BOT_LOG_CHANNEL_ID) as SocketTextChannel;

            EmbedBuilder eb = new() {Color = colours[(int) level], Title = message};
            eb.WithFooter(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss tt"));
            if (author != null)
                eb.WithAuthor(author);

            await channel.SendMessageAsync(embed: eb.Build());
        }

        public async Task SetLogLevel(SocketMessage msg) {
            string levelToToggle = msg.Content.Split(" ")[2];
            if (Enum.TryParse(levelToToggle, out LogLevel lvl)) {
                loggingLevel ^= lvl;
                string s = ((loggingLevel & lvl) == lvl) ? "Enabled" : "Disabled";
                await Log(LogLevel.FATAL, $"{s} log level {lvl}");
                return;
            }

            await Log(LogLevel.FATAL, "Failed to parse loglevel: " + levelToToggle);
        }

        public async Task Initialise() {
            client = new DiscordSocketClient();
            client.Log += msg => {
                Console.WriteLine(msg.ToString());
                // await Log(MapSeverityToLevel(msg.Severity), msg.Message);
                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();

            client.Ready += () => {
                client.MessageReceived += GetContext().MessageListener.ToCastedFunc<SocketMessage>();
                AddModule<EmojiModule>();

                return Task.CompletedTask;
            };

        }

        private LogLevel MapSeverityToLevel(LogSeverity severity) {
            return severity switch {
                LogSeverity.Critical => LogLevel.FATAL,
                LogSeverity.Error => LogLevel.ERROR,
                LogSeverity.Warning => LogLevel.ERROR,
                LogSeverity.Info => LogLevel.INFO,
                LogSeverity.Verbose => LogLevel.DEBUG,
                LogSeverity.Debug => LogLevel.DEBUG,
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
            };
        }

        public DiscordSocketClient GetClient() {
            return client;
        }
    }
}