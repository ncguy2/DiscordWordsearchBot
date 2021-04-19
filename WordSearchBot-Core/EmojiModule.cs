using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core {
    public class EmojiModule : Module {
        public enum RequestType {
            None,
            Attachment,
            Emoji,
            Embed
        }

        protected readonly ulong ChannelId = ConfigKeys.Emoji.SUGGESTION_CHANNEL_ID.Get();
        protected readonly int VoteThreshold = ConfigKeys.Emoji.VOTE_THRESHOLD.Get();

        protected MessageList suggestedList;

        public override string DisplayName() {
            return "Emoji";
        }

        public override string InternalName() {
            return "emoji";
        }

        public override void RegisterEvents(DiscordContext context) {
            context.MessageListener
                   .Make()
                   .AddPredicate(Predicates.FilterOnBotMessage())
                   .AddPredicate(Predicates.FilterOnChannelId(ChannelId))
                   .AddPredicate(Predicates.FilterOnMention(context.Client.CurrentUser))
                   .AddPredicate(Predicates.FilterOnCommandPattern("emoji", "add"))
                   .AddTask(SuggestEmoji);

            context.MessageListener
                   .Make()
                   .AddPredicate(Predicates.FilterOnBotMessage())
                   .AddPredicate(Predicates.FilterOnMention(context.Client.CurrentUser))
                   .AddPredicate(Predicates.FilterOnCommandPattern("emoji", "list"))
                   .AddTask(ListSuggestions);

            context.MessageListener
                   .Make()
                   .AddPredicate(Predicates.FilterOnBotMessage())
                   .AddPredicate(Predicates.FilterOnChannelId(ChannelId))
                   .AddPredicate(Predicates.FilterOnMention(context.Client.CurrentUser))
                   .AddPredicate(Predicates.FilterOnCommandPattern("emoji", "process", "reply"))
                   .AddTask(ProcessReply);


            SocketChannel socketChannel = context.Client.GetChannel(ConfigKeys.Emoji.SUGGESTION_CHANNEL_ID.Get());
            suggestedList = new MessageList(CacheFile("suggestions.list"),
                                            socketChannel as SocketTextChannel);

            context.Client.ReactionAdded += CheckSuggestion;
        }

        private async Task ProcessReply(IUserMessage arg) {
            if (arg.ReferencedMessage == null) {
                Throw("No referenced message, please reply to the message I should process first");
                return;
            }

            await SuggestEmoji(arg.ReferencedMessage);
        }

        private async Task CheckSuggestion(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
                                           SocketReaction reaction) {
            if (channel.Id != ChannelId)
                return;

            if (!suggestedList.Contains(msg.Id))
                return;

            IUserMessage message = await msg.GetOrDownloadAsync();

            IReadOnlyDictionary<IEmote, ReactionMetadata> readOnlyDictionary = message.Reactions;
            List<ulong> seenUserIds = new();
            int userCount = 0;
            foreach (KeyValuePair<IEmote, ReactionMetadata> pair in readOnlyDictionary) {
                IAsyncEnumerable<IReadOnlyCollection<IUser>> reactionUsersAsync =
                    message.GetReactionUsersAsync(pair.Key, 100);
                await foreach (IReadOnlyCollection<IUser> readOnlyCollection in reactionUsersAsync)
                foreach (IUser user in readOnlyCollection) {
                    if (seenUserIds.Contains(user.Id) || user.IsBot)
                        continue;
                    seenUserIds.Add(user.Id);
                    userCount++;
                }
            }

            if (userCount >= VoteThreshold) {
                suggestedList.Remove(message);
                await AddEmoji(message);
            }
        }

        private async Task SuggestEmoji(IUserMessage msg) {
            RequestType requestType = DetermineRequestType(msg);
            if (requestType == RequestType.None)
                return;

            suggestedList.Add(msg);
            await msg.AddReactionAsync(EmojiHelper.getEmote(AssignedCore.GetClient(), "thumbsup").asEmote());

            await msg.ReplyAsync(
                $"Emoji successfully submitted for suggestion. Please vote with reactions, a minimum of {VoteThreshold + 1} distinct votes are required",
                allowedMentions: AllowedMentions.None);
        }

        private async Task ListSuggestions(IUserMessage msg) {
            List<IUserMessage> msgs = suggestedList.AsList();
            if (msgs.Count == 0) {
                await msg.ReplyAsync("No outstanding suggestions.");
                return;
            }

            EmbedBuilder eb = new();

            Dictionary<IUser, List<IUserMessage>> groupedMessages = new();

            eb.WithTitle("Outstanding suggestions");

            foreach (IUserMessage m in msgs) {
                if(!groupedMessages.ContainsKey(m.Author))
                    groupedMessages.Add(m.Author, new List<IUserMessage>());

                groupedMessages[m.Author].Add(m);
            }

            // List<EmbedFieldBuilder> fields = msgs
            //                                  .Select(m => new EmbedFieldBuilder().WithName(m.GetJumpUrl())
            //                                              .WithValue($"From {m.Author.Mention}")).ToList();

            List<EmbedFieldBuilder> fields = new();
            foreach (KeyValuePair<IUser,List<IUserMessage>> p in groupedMessages) {
                EmbedFieldBuilder b = new EmbedFieldBuilder().WithName($"From {p.Key.Mention}");
                string v = "";
                for (int index = 0; index < p.Value.Count; index++) {
                    IUserMessage uMsg = p.Value[index];
                    if (index > 0)
                        v += "\n";
                    v += uMsg.GetJumpUrl();
                }

                b.WithValue(v);
                fields.Add(b);
            }

            eb.WithFields(fields);

            await msg.ReplyAsync(embed: eb.Build());
        }

        private RequestType DetermineRequestType(IUserMessage msg) {
            GetKeyFromMessage(msg.Content);

            if (msg.Attachments.Count > 0)
                return RequestType.Attachment;
            if (msg.Tags.Any(x => x.Type == TagType.Emoji))
                return RequestType.Emoji;
            if (LinkFinder.GetUrlsFromString(msg.Content).Count > 0)
                return RequestType.Embed;

            return RequestType.None;
        }

        private Func<IUserMessage, Task<bool>> GetTaskFromType(RequestType type) {
            switch (type) {
                case RequestType.Attachment:
                    return AddEmojiFromAttachment;
                case RequestType.Emoji:
                    return AddEmojiFromExistingEmoji;
                case RequestType.Embed:
                    return AddEmojiFromEmbed;
            }

            Throw("Unable to get request type");
            return null;
        }

        private async Task AddEmoji(IUserMessage msg) {
            RequestType type = DetermineRequestType(msg);
            Func<IUserMessage, Task<bool>> task = GetTaskFromType(type);

            try {
                bool success = await task(msg);
                if (success)
                    await msg.ReplyAsync("Successfully added this as an emoji, enjoy using your new toy.",
                                         allowedMentions: AllowedMentions.None);
            } catch (ModuleException e) {
                await msg.ReplyAsync("Something went wrong here, please let someone know.");
                // ReSharper disable once CA2200
                throw e;
            }
        }

        private async Task<bool> AddEmojiFromEmbed(IUserMessage msg) {
            List<string> urlsFromString = LinkFinder.GetUrlsFromString(msg.Content);
            if (urlsFromString.Count >= 2)
                Throw("Too many links in a single message, separate them out please");

            if (urlsFromString.Count <= 0)
                Throw("Attempted to add emoji from embed, but couldn't find a link in the given message");

            string url = urlsFromString[0];
            string key = GetKeyFromMessage(msg.Content);
            if (key == null) {
                key = StringUtils.GetFileNameFromFilePathOrUrl(url);
                key = StringUtils.RemoveExtension(key);
            }

            await AddEmojiFromUrl(((SocketGuildChannel) msg.Channel).Guild, key, url);

            return true;
        }

        private async Task<bool> AddEmojiFromAttachment(IUserMessage msg) {
            if (msg.Attachments.Count > 1)
                Throw("Too many attachments in a single message, separate them out please");

            if (msg.Attachments.Count <= 0)
                Throw("Attempted to add emoji from attachment, but couldn't find an attachment in the given message");

            IAttachment attachment = msg.Attachments.ToArray()[0];
            string key = GetKeyFromMessage(msg.Content);
            if (key == null) {
                key = StringUtils.GetFileNameFromFilePathOrUrl(attachment.Filename);
                key = StringUtils.RemoveExtension(key);
            }

            await AddEmojiFromUrl(((SocketGuildChannel) msg.Channel).Guild, key, attachment.Url);

            return true;
        }

        private string GetKeyFromMessage(string message) {
            message = message.Replace("emoji", "");
            message = message.Replace("add", "");
            message = message.Replace("process", "");
            message = message.Replace("reply", "");
            message = StringUtils.RemoveCrap(message);

            Regex formatRx = new(@"[a-zA-Z0-9_]", RegexOptions.Compiled);

            string[] segs = message.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            List<string> candidates = segs.Where(seg => seg.Length <= 32).Where(seg => formatRx.IsMatch(seg)).ToList();

            if (candidates.Count == 0) {
                Log(Core.LogLevel.ERROR, "Cannot find suitable candidate for emoji name");
                return null;
            }

            return candidates[0];
        }

        private async Task<bool> AddEmojiFromExistingEmoji(IUserMessage msg) {
            ITag? tag = msg.Tags.FirstOrDefault(x => x.Type == TagType.Emoji);

            if (tag == null)
                Throw("Attempted to add emoji from existing, but couldn't find an emoji in the given message");
            Tag<Emote> emoteTag = tag as Tag<Emote>;
            Emote emote = emoteTag.Value;

            string key = GetKeyFromMessage(msg.Content) ?? emote.Name;
            await AddEmojiFromUrl(((SocketGuildChannel) msg.Channel).Guild, key, emote.Url);
            return true;
        }

        private async Task AddEmojiFromUrl(IGuild guild, string emoteKey, string url) {
            using WebClient client = new();
            Directory.CreateDirectory(".downloads");
            string filePath = $".downloads\\{emoteKey}.{GetFileExt(url)}";
            client.DownloadFile(url, filePath);

            await AddEmojiFromFile(guild, emoteKey, filePath);
        }

        private async Task AddEmojiFromFile(IGuild guild, string key, string file) {
            string fullPath = Path.GetFullPath(file);
            Image img = new(fullPath);

            await Log(Core.LogLevel.DEBUG, $"Adding emoji {key}. [File: {fullPath}]");
            Task<GuildEmote> task = guild.CreateEmoteAsync(key, img);
            GuildEmote emoteAsync = task.GetAwaiter().GetResult();

            if (emoteAsync == null) await Log(Core.LogLevel.ERROR, $"Failed to add emoji with key {key}");
        }

        private static string GetFileExt(string file) {
            int lastIndexOf = file.LastIndexOf(".");
            return lastIndexOf <= 0 ? ".unknown" : file.Substring(lastIndexOf + 1);
        }
    }
}