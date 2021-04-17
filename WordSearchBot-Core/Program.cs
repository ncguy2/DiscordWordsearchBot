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

        private DiscordSocketClient client;
        private readonly string Token = GlobalConfig.GetValue(ConfigKeys.TOKEN).GetString(false, false);
        private ulong ID = 813538277725438012;

        private readonly ulong BOT_TESTING_CHANNEL_ID = 825059509561982976;
        private readonly ulong BOT_LOG_CHANNEL_ID = 833101022452908062;

        [Flags]
        private enum LogLevel {
            FATAL,
            ERROR,
            INFO,
            DEBUG
        }

        private LogLevel loggingLevel = LogLevel.ERROR | LogLevel.INFO | LogLevel.DEBUG;

        private Color[] colours = {
            Color.Red,
            Color.Orange,
            Color.Blue,
            Color.Green
        };

        private async Task LogMessage(LogLevel level, string message) {

            if (level != LogLevel.FATAL && (loggingLevel & level) == 0)
                return;

            SocketTextChannel channel = client.GetChannel(BOT_LOG_CHANNEL_ID) as SocketTextChannel;

            EmbedBuilder eb = new ();
            eb.Color = colours[(int) level];
            eb.Title = message;
            eb.WithFooter(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss tt"));

            channel.SendMessageAsync(embed: eb.Build());
        }

        public async Task MainAsync() {
            client = new DiscordSocketClient();
            client.Log += Log;

            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();

            client.Ready += async () => {
                LogMessage(LogLevel.INFO, "Client is ready!");

                EventListener<SocketMessage> emojiListener = new();

                emojiListener.AddPredicate(FilterOnChannelId(BOT_TESTING_CHANNEL_ID))
                             .AddPredicate(FilterOnMention(client.CurrentUser))
                             .AddTask(AddEmoji);

                client.MessageReceived += emojiListener.ToFunc();

                EventListener<SocketMessage> logLevelListener = new();

                logLevelListener.AddPredicate(FilterOnChannelId(BOT_TESTING_CHANNEL_ID))
                                .AddPredicate(FilterOnMention(client.CurrentUser))
                                .AddTask(SetLogLevel);

                    client.MessageReceived += logLevelListener.ToFunc();
            };

            await Task.Delay(-1);
        }

        private async Task SetLogLevel(SocketMessage msg) {
            string levelToToggle = msg.Content.Split(" ")[2];
            if (LogLevel.TryParse(levelToToggle, out LogLevel lvl)) {
                loggingLevel ^= lvl;
                string s = ((loggingLevel & lvl) == lvl) ? "Enabled" : "Disabled";
                LogMessage(LogLevel.FATAL, $"{s} log level {lvl}");
                return;
            }

            LogMessage(LogLevel.FATAL, "Failed to parse loglevel: " + levelToToggle);
        }

        private async Task AddEmoji(SocketMessage msg) {
            LogMessage(LogLevel.DEBUG, "Hello, message");
            AddEmojiFromExisting(msg);
        }

        private async Task AddEmojiFromExisting(SocketMessage msg) {
            ITag? tag = msg.Tags.FirstOrDefault(x => x.Type == TagType.Emoji);

            if(tag == null) {
                LogMessage(LogLevel.ERROR,
                           "Attempted to add emoji from existing, but couldn't find an emoji in the given message");
                return;
            }
            Tag<Emote> emoteTag = tag as Tag<Emote>;
            Emote emote = emoteTag.Value;

            using WebClient client = new ();
            Directory.CreateDirectory(".downloads");
            string filePath = $".downloads\\{emote.Name}.{GetFileExt(emote.Url)}";
            client.DownloadFile(emote.Url, filePath);

            Image img = new(filePath);

            string message = msg.Content;
            Regex rx = new(@"<.*?>", RegexOptions.Compiled);
            Regex spaceRx = new(@"\s+", RegexOptions.Compiled);

            message = rx.Replace(message, "");
            message = spaceRx.Replace(message, " ");
            message = message.Trim();

            string[] msgContent = message.Split(" ");
            string key = msgContent[0];

            string fullPath = Path.GetFullPath(filePath);

            LogMessage(LogLevel.DEBUG, $"Adding emoji {emote.Name} with custom key {key}. [File: {fullPath}]");

            await (msg.Channel as SocketTextChannel).Guild.CreateEmoteAsync(key, img);
        }

        private static string GetFileExt(string file) {
            int lastIndexOf = file.LastIndexOf(".");
            return lastIndexOf <= 0 ? ".unknown" : file.Substring(lastIndexOf + 1);
        }

        private Task Log(LogMessage msg) {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static Func<SocketMessage, bool> FilterOnMention(SocketUser user) {
            return t => t.MentionedUsers.Select(x => x.Id).Contains(user.Id);
        }

        private static Func<SocketMessage, bool> FilterOnChannelId(ulong channelId) {
            return t => t.Channel.Id == channelId;
        }

    }
}