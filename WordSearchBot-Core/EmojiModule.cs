using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using WordSearchBot.Core.Data.Facade;
using WordSearchBot.Core.Model;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core {
    public class EmojiModule : Module {
        public enum RequestType {
            None,
            Attachment,
            Emoji,
            Embed
        }

        [Flags]
        public enum ValidityStatus {
            Valid = 0,
            File_Size_Too_Big = 0b0001,
            Invalid_Name = 0b0010,
            Name_Too_Short = Invalid_Name,
            Name_Not_Alphanumeric = Invalid_Name | 0b0100,
            Insufficient_Slots = 0b1000,
            Unknown_Error = 0b0100_0000,
            Silent_Error = 0b1000_0000,
        }

        public bool Mask(ValidityStatus value, ValidityStatus mask) {
            return (value & mask) == mask;
        }

        public ValidityStatus[] ValidityStatusArray = {
            ValidityStatus.File_Size_Too_Big,
            ValidityStatus.Invalid_Name,
            ValidityStatus.Name_Too_Short,
            ValidityStatus.Name_Not_Alphanumeric,
            ValidityStatus.Insufficient_Slots
        };

        public int GetEmoteLimit(IGuild guild) {
            return guild.PremiumTier switch {
                PremiumTier.None => 50,
                PremiumTier.Tier1 => 100,
                PremiumTier.Tier2 => 150,
                PremiumTier.Tier3 => 250,
                _ => 50
            };
        }

        protected readonly ulong ChannelId = ConfigKeys.Emoji.SUGGESTION_CHANNEL_ID.Get();
        protected readonly int VoteThreshold = ConfigKeys.Emoji.VOTE_THRESHOLD.Get();

        protected MessageList suggestedList;
        protected Suggestions suggestions;

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
                   .AddPredicate(Predicates.FilterOnCommandPattern("emoji", "inspect"))
                   .AddTask(InspectEmojiCmd);

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

            context.MessageListener
                   .Make()
                   .AddPredicate(Predicates.FilterOnBotMessage())
                   .AddPredicate(Predicates.FilterOnMention(context.Client.CurrentUser))
                   .AddPredicate(Predicates.FilterOnCommandPattern("emoji", "migrate"))
                   .AddTask(MigrateItems);

            SocketChannel socketChannel = context.Client.GetChannel(ConfigKeys.Emoji.SUGGESTION_CHANNEL_ID.Get());
            suggestedList = new MessageList(CacheFile("suggestions.list"),
                                            socketChannel as SocketTextChannel);
            suggestions = new Suggestions();

            context.Client.ReactionAdded += (msg, channel, reaction) => {
                Task.Run(() => CheckSuggestion(msg, channel, reaction));
                return Task.CompletedTask;
            };
        }

        private async Task MigrateItems(IUserMessage msg) {
            if (msg.Channel.Id != Core.BOT_TESTING_CHANNEL_ID)
                return;

            List<IUserMessage> userMessages = suggestedList.AsList();
            List<IUserMessage> toRemove = new();

            MessageUtils.LongTaskMessage(msg, $"Messages to migrate: {userMessages.Count}", async (_, prog) => {
                foreach (IUserMessage m in userMessages) {
                    await prog.ModifyAsync(p => {
                        p.Content = "Migrating message: " + m.Id;
                    });
                    if (await MigrateItem(m))
                        toRemove.Add(m);
                }

                foreach (IUserMessage userMessage in toRemove)
                    suggestedList.Remove(userMessage);

                LongTaskMessageReturn longTaskMessageReturn = new() {
                    strContent = $"Migration completed. Remaining legacy suggestions: {suggestedList.AsList().Count}"
                };
                return longTaskMessageReturn;
            });
        }

        private async Task<bool> MigrateItem(IUserMessage msg) {
            IAsyncEnumerable<IReadOnlyCollection<IMessage>> msgs = msg.Channel.GetMessagesAsync(msg, Direction.After, 4);

            async Task<IMessage> GetReply(IAsyncEnumerable<IReadOnlyCollection<IMessage>> asyncEnumerable) {
                await foreach (IReadOnlyCollection<IMessage> m in asyncEnumerable) {
                    foreach (IMessage message in m) {
                        if (message.Author.Id != AssignedCore.GetClient().CurrentUser.Id) continue;
                        return message;
                    }
                }

                return null;
            }

            IMessage reply = await GetReply(msgs);

            Suggestion suggestion = suggestions.Create(msg);
            suggestion.InitialReplyId = reply.Id;
            suggestions.Update(suggestion);

            return true;
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

            bool isLegacy = false;

            if (!suggestedList.Contains(msg.Id) && !suggestions.ContainsAndIs(msg.Id, x => x.Status == VoteStatus.Pending))
                return;

            isLegacy = suggestedList.Contains(msg.Id);

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
                if(isLegacy)
                    suggestedList.Remove(message);
                else {
                    Suggestion suggestion = suggestions.GetFromMessageId(msg.Id);
                    suggestion.Status = VoteStatus.Passed;
                    suggestions.Update(suggestion);
                }
                await AddEmoji(message);
            }
        }

        private async Task InspectEmojiCmd(IUserMessage msg) {
            ValidityStatus validityStatus = await InspectEmoji(msg);

            string reply = "";

            if (Mask(validityStatus, ValidityStatus.Silent_Error))
                return;

            if (validityStatus == ValidityStatus.Valid) {
                reply = "All looks good";
            } else {
                reply = "Some errors were found";
                reply += GetErrorMessages(validityStatus);
            }

                // reply += "Emoji validity:\n";
            // reply += "Valid: " + ((validityStatus == 0) ? "True" : "False") + "\n";
            // reply += "Invalid: " + ((validityStatus > 0) ? "True" : "False") + "\n";
            // foreach (ValidityStatus s in ValidityStatusArray) {
            //     string name = Enum.GetName(s);
            //     reply += $"{name}: " + ((validityStatus & s) == s ? "True" : "False") + "\n";
            // }

            await msg.ReplyAsync(reply);
        }

        private async Task<ValidityStatus> InspectEmoji(IUserMessage msg) {
            ITextChannel textChannel = msg.Channel as ITextChannel;
            if (textChannel == null)
                return ValidityStatus.Unknown_Error;
            IGuild guild = textChannel.Guild;
            int emoteLimit = GetEmoteLimit(guild);

            RequestType type = DetermineRequestType(msg);

            if (type == RequestType.None && msg.ReferencedMessage != null) {
                msg = msg.ReferencedMessage;
                type = DetermineRequestType(msg);
            }

            string key = GetKeyFromMessage(msg.Content);

            ValidityStatus flags = 0;

            if (key.Length < 2)
                flags |= ValidityStatus.Name_Too_Short;

            // TODO add support for checking the name
            // if(!new Regex("[a-zA-Z0-9_]").IsMatch())
                // nameFlags |= ValidityStatus.Name_Not_Alphanumeric;

            if (guild.Emotes.Count == emoteLimit)
                flags |= ValidityStatus.Insufficient_Slots;

            const int maxFileSize = 262144; // 256KB
            switch (type) {
                case RequestType.Attachment:
                    IAttachment attachment = msg.Attachments.ToArray()[0];
                    if (attachment.Size > maxFileSize)
                        flags |= ValidityStatus.File_Size_Too_Big;
                    break;
                case RequestType.Embed:
                    string url = LinkFinder.GetUrlsFromString(msg.Content)[0];

                    try {
                        await DownloadTempFile(url, file => {
                            if (file.Length > maxFileSize)
                                flags |= ValidityStatus.File_Size_Too_Big;
                        });
                    } catch (WebException we) {
                        msg.ReplyAsync(we.Message);
                        return ValidityStatus.Silent_Error;
                    }

                    break;
            }

            return flags;
        }

        private string GetErrorMessages(ValidityStatus validity) {
            string replyMsg = "";

            if (Mask(validity, ValidityStatus.File_Size_Too_Big))
                replyMsg += "\n  - The file is too big, it must be below 256KB.";
            if(Mask(validity, ValidityStatus.Name_Too_Short))
                replyMsg += "\n  - The name given or inferred is too short, it must be at least 2 characters long";
            if(Mask(validity, ValidityStatus.Name_Not_Alphanumeric))
                replyMsg += "\n  - The name given or inferred has invalid characters, it can only contain alphanumeric characters and underscores";
            if(Mask(validity, ValidityStatus.Insufficient_Slots))
                replyMsg += "\n  - There aren't enough slots left on the server, please ask an admin to look into clearing some out.";
            if(Mask(validity, ValidityStatus.Unknown_Error))
                replyMsg += "\n  - Unknown error, no idea what broke here ¯\\_(ツ)_/¯";

            return replyMsg;
        }

        private async Task SuggestEmoji(IUserMessage msg) {
            RequestType requestType = DetermineRequestType(msg);
            if (requestType == RequestType.None)
                return;

            ValidityStatus validity = await InspectEmoji(msg);

            if (validity == ValidityStatus.Valid) {
                Suggestion suggestion = suggestions.Create(msg);
                await msg.AddReactionAsync(EmojiHelper.getEmote(AssignedCore.GetClient(), "thumbsup").asEmote());

                IUserMessage reply = await msg.ReplyAsync(
                    $"Emoji successfully submitted for suggestion. Please vote with reactions, a minimum of {VoteThreshold + 1} distinct votes are required",
                    allowedMentions: AllowedMentions.None);

                suggestion.InitialReplyId = reply.Id;
                suggestions.Insert(suggestion);
                return;
            }

            string replyMsg = "Some issues were discovered with this suggestion.";

            replyMsg += GetErrorMessages(validity);

            await msg.ReplyAsync(replyMsg);

        }

        private Task ListSuggestions(IUserMessage msg) {
            MessageUtils.LongTaskMessage(msg, "Fetching outstanding suggestions", async (_, prog) => {
                List<IUserMessage> msgs = suggestedList.AsList();
                List<Suggestion> sugs = suggestions.Get(s => s.InternalStatus == (byte) VoteStatus.Pending).ToList();

                if (msgs.Count == 0 && sugs.Count == 0)
                    return new LongTaskMessageReturn("No outstanding suggestions");

                EmbedBuilder eb = new();

                Dictionary<IUserWrapper, List<IUserMessage>> groupedMessages = new();

                eb.WithTitle("Outstanding suggestions");

                await prog.ModifyAsync(p => {
                    p.Content = "Grouping suggestions...";
                });

                foreach (IUserMessage m in msgs) {
                    if (m == null)
                        continue;
                    IUserWrapper wrapper = new(m.Author);
                    if (!groupedMessages.ContainsKey(wrapper))
                        groupedMessages.Add(wrapper, new List<IUserMessage>());

                    groupedMessages[wrapper].Add(m);
                }

                foreach (Suggestion sug in sugs) {
                    IUserWrapper wrapper = new(sug.GetMessage(msg.Channel).Author);
                    if (!groupedMessages.ContainsKey(wrapper))
                        groupedMessages.Add(wrapper, new List<IUserMessage>());

                    groupedMessages[wrapper].Add(sug.GetMessage(msg.Channel));
                }

                await prog.ModifyAsync(p => {
                    p.Content = "Building embed...";
                });

                List<EmbedFieldBuilder> fields = new();
                foreach (KeyValuePair<IUserWrapper, List<IUserMessage>> p in groupedMessages) {
                    EmbedFieldBuilder b = new EmbedFieldBuilder().WithName($"From {p.Key.User.Username}");
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
                eb.WithColor(Color.Blue);

                return new LongTaskMessageReturn(eb.Build());
            });
            return Task.CompletedTask;
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
            } catch (HttpException he) {
                string reply = he.Message + "\n - ";
                reply += he.Reason ?? "No reason provided";
                await msg.ReplyAsync(reply);
            } catch (ModuleException e) {
                await msg.ReplyAsync("Something went wrong here, please let someone know.");
                // ReSharper disable once CA2200
                throw e;
            } catch (Exception e) {
                await msg.ReplyAsync(e.Message);
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
            message = message.Replace("migrate", "");
            message = message.Replace("reply", "");
            message = StringUtils.RemoveCrap(message);

            Regex formatRx = new(@"[a-zA-Z0-9_]", RegexOptions.Compiled);

            string[] segs = message.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            List<string> candidates = segs.Select(StringUtils.GetFileNameFromFilePathOrUrl).Where(seg => seg.Length <= 32).Where(seg => formatRx.IsMatch(seg)).ToList();

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
            AddEmojiFromUrl(((SocketGuildChannel) msg.Channel).Guild, key, emote.Url);
            return true;
        }

        private async Task AddEmojiFromUrl(IGuild guild, string emoteKey, string url) {
            string filePath = await DownloadFile(url, emoteKey);
            await AddEmojiFromFile(guild, emoteKey, filePath);
        }

        private async Task<string> DownloadFile(string url, string filename) {
            await Log(Core.LogLevel.DEBUG, $"Downloading file from \"{url}\" to \"{filename}\"");
            using WebClient client = new();
            Directory.CreateDirectory(".downloads");
            string filePath = $".downloads/{filename}.{GetFileExt(url)}";
            client.DownloadFile(url, filePath);

            return filePath;
        }

        private async Task DownloadTempFile(string url, Action<FileInfo> callback) {
            string downloadFile = await DownloadFile(url, StringUtils.RandomString(8));
            callback(new FileInfo(downloadFile));
            File.Delete(downloadFile);
        }

        private async Task<T> DownloadTempFile<T>(string url, Func<FileInfo, T> callback) {
            string downloadFile = await DownloadFile(url, StringUtils.RandomString(8));
            T val = callback(new FileInfo(downloadFile));
            File.Delete(downloadFile);
            return val;
        }

        private async Task AddEmojiFromFile(IGuild guild, string key, string file) {
            string fullPath = Path.GetFullPath(file);
            Image img = new(fullPath);

            await Log(Core.LogLevel.DEBUG, $"Adding emoji {key}. [File: {fullPath}]");
            GuildEmote task = await guild.CreateEmoteAsync(key, img);

            if (task == null) await Log(Core.LogLevel.ERROR, $"Failed to add emoji with key {key}");
        }

        private static string GetFileExt(string file) {
            int lastIndexOf = file.LastIndexOf(".");
            return lastIndexOf <= 0 ? ".unknown" : file.Substring(lastIndexOf + 1);
        }
    }

    public struct IUserWrapper {
        public IUser User;
        public ulong Id;

        public IUserWrapper(IUser user) {
            User = user;
            Id = user.Id;
        }

        public bool Equals(IUserWrapper other) {
            return Id == other.Id;
        }

        public override bool Equals(object obj) {
            return obj is IUserWrapper other && Equals(other);
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }
    }

}