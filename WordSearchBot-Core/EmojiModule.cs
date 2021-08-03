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
using WordSearchBot.Core.Data;
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
            Valid,
            File_Size_Too_Big,
            Name_Too_Short,
            Name_Not_Alphanumeric,
            Insufficient_Slots,
            Unknown_Error,
            Silent_Error
        }

        protected readonly ulong ChannelId = ConfigKeys.Emoji.SUGGESTION_CHANNEL_ID.Get();
        protected readonly int VoteThreshold = ConfigKeys.Emoji.VOTE_THRESHOLD.Get();

        protected MessageList suggestedList;
        protected Suggestions suggestions;

        public ValidityStatus[] ValidityStatusArray = {
            ValidityStatus.File_Size_Too_Big,
            ValidityStatus.Name_Too_Short,
            ValidityStatus.Name_Not_Alphanumeric,
            ValidityStatus.Insufficient_Slots
        };

        public static bool Mask(ValidityStatus value, ValidityStatus mask) {
            return (value & mask) == mask;
        }

        public int GetEmoteLimit(IGuild guild) {
            return guild.PremiumTier switch {
                PremiumTier.None => 50,
                PremiumTier.Tier1 => 100,
                PremiumTier.Tier2 => 150,
                PremiumTier.Tier3 => 250,
                _ => 50
            };
        }

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
                TaskUtils.Run(() => CheckSuggestion(msg, channel, reaction));
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
                    await prog.ModifyAsync(p => { p.Content = "Migrating message: " + m.Id; });
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
            IAsyncEnumerable<IReadOnlyCollection<IMessage>>
                msgs = msg.Channel.GetMessagesAsync(msg, Direction.After, 4);

            async Task<IMessage> GetReply(IAsyncEnumerable<IReadOnlyCollection<IMessage>> asyncEnumerable) {
                await foreach (IReadOnlyCollection<IMessage> m in asyncEnumerable)
                foreach (IMessage message in m) {
                    if (message.Author.Id != AssignedCore.GetClient().CurrentUser.Id) continue;
                    return message;
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

            if (!suggestions.ContainsAndIs(msg.Id, x => x.Status == VoteStatus.Pending))
                return;

            IUserMessage message = await msg.GetOrDownloadAsync();

            IntegrityCheckForMessage(message);

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
                Suggestion suggestion = suggestions.GetFromMessageId(msg.Id);
                suggestion.Status = VoteStatus.Passed;
                suggestions.Update(suggestion);
                await AddEmoji(message);
            }
        }

        private async Task IntegrityCheckForMessage(IUserMessage msg) {
            Suggestion[] arr = Storage.Get<Suggestion>(x => x.Status == VoteStatus.Pending && x.MessageId == msg.Id).ToArray();
            int amt = arr.Length;
            if (amt <= 1)
                return;

            msg.AddReactionAsync(EmojiHelper.getEmote(AssignedCore.GetClient(), "confused").asEmote());
            // Leave the first suggestion as-is, mark the rest as Erroneous
            for (int i = 1; i < arr.Length; i++) {
                arr[i].Status = VoteStatus.Erroneous;
                arr[i].Update();
                MarkMessageAsErroneous(arr[i].GetReply(msg.Channel));
            }
        }

        private async Task MarkMessageAsErroneous(IUserMessage replyMsg) {
            await replyMsg.ModifyAsync(x => {
                x.Content = "Duplicate suggestion, cleaning up...";
            });
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

            RequestType type = DetermineRequestType(msg, Log);

            if (type == RequestType.None && msg.ReferencedMessage != null) {
                msg = msg.ReferencedMessage;
                type = DetermineRequestType(msg, Log);
            }

            string key = GetKeyFromMessage(msg.Content, Log);

            ValidityStatus flags = 0;

            if (key.Length < 2)
                flags |= ValidityStatus.Name_Too_Short;

            if(new Regex("[^a-zA-Z0-9_]").IsMatch(key))
                flags |= ValidityStatus.Name_Not_Alphanumeric;

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
                        DownloadHelper.DownloadTempFile(url, file => {
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

        public static string GetErrorMessages(ValidityStatus validity) {
            string replyMsg = "";

            if (Mask(validity, ValidityStatus.File_Size_Too_Big))
                replyMsg += "\n  - The file is too big, it must be below 256KB.";
            if (Mask(validity, ValidityStatus.Name_Too_Short))
                replyMsg += "\n  - The name given or inferred is too short, it must be at least 2 characters long";
            if (Mask(validity, ValidityStatus.Name_Not_Alphanumeric))
                replyMsg +=
                    "\n  - The name given or inferred has invalid characters, it can only contain alphanumeric characters and underscores";
            if (Mask(validity, ValidityStatus.Insufficient_Slots))
                replyMsg +=
                    "\n  - There aren't enough slots left on the server, please ask an admin to look into clearing some out.";
            if (Mask(validity, ValidityStatus.Unknown_Error))
                replyMsg += "\n  - Unknown error, no idea what broke here ¯\\_(ツ)_/¯";

            return replyMsg;
        }

        private async Task SuggestEmoji(IUserMessage msg) {
            if (IsMessageAlreadyHandled(msg)) {
                msg.AddReactionAsync(EmojiHelper.getEmote(AssignedCore.GetClient(), "confused").asEmote());
                return;
            }

            RequestType requestType = DetermineRequestType(msg, Log);
            if (requestType == RequestType.None)
                return;

            ValidityStatus validity = await InspectEmoji(msg);

            if (validity == ValidityStatus.Valid) {
                Suggestion suggestion = suggestions.Create(msg);
                msg.AddReactionAsync(EmojiHelper.getEmote(AssignedCore.GetClient(), "thumbsup").asEmote());

                IUserMessage reply = await msg.ReplyAsync(
                    $"Emoji successfully submitted for suggestion. Please vote with reactions, a minimum of {VoteThreshold + 1} distinct votes are required",
                    allowedMentions: AllowedMentions.None);

                suggestion.InitialReplyId = reply.Id;
                suggestions.Insert(suggestion);
                return;
            }

            string replyMsg = "Some issues were discovered with this suggestion.";

            replyMsg += GetErrorMessages(validity);

            msg.ReplyAsync(replyMsg);
        }

        private bool IsMessageAlreadyHandled(IUserMessage msg) {
            return suggestions.Contains(msg.Id);
        }

        private Task ListSuggestions(IUserMessage msg) {
            MessageUtils.LongTaskMessage(msg, "Fetching outstanding suggestions", async (_, prog) => {
                List<Suggestion> sugs = suggestions.Get(s => s.InternalStatus == (byte) VoteStatus.Pending).ToList();

                if (sugs.Count == 0)
                    return new LongTaskMessageReturn("No outstanding suggestions");

                EmbedBuilder eb = new();

                Dictionary<IUserWrapper, List<IUserMessage>> groupedMessages = new();

                eb.WithTitle("Outstanding suggestions");

                await prog.ModifyAsync(p => { p.Content = "Grouping suggestions..."; });

                foreach (Suggestion sug in sugs) {
                    IUserWrapper wrapper = new(sug.GetMessage(msg.Channel).Author);
                    if (!groupedMessages.ContainsKey(wrapper))
                        groupedMessages.Add(wrapper, new List<IUserMessage>());

                    groupedMessages[wrapper].Add(sug.GetMessage(msg.Channel));
                }

                await prog.ModifyAsync(p => { p.Content = "Building embed..."; });

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

        public static RequestType DetermineRequestType(IUserMessage msg) {
            return DetermineRequestType(msg, (level, s) => { Console.WriteLine($"[{level}] {s}"); });
        }

        public static RequestType DetermineRequestType(IUserMessage msg, Action<Core.LogLevel, string> Log) {
            Func<Core.LogLevel, string, Task> l = (level, s) => {
                Log(level, s);
                return Task.CompletedTask;
            };

            return DetermineRequestType(msg, l);
        }

        public static RequestType DetermineRequestType(IUserMessage msg, Func<Core.LogLevel, string, Task> Log) {
            GetKeyFromMessage(msg.Content, Log);

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

            ValidityStatus validity = await InspectEmoji(msg);

            if (validity != ValidityStatus.Valid) {
                string replyMsg = "Some issues were discovered with this suggestion.";
                replyMsg += GetErrorMessages(validity);
                msg.ReplyAsync(replyMsg);
                return;
            }

            RequestType type = DetermineRequestType(msg, Log);
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
            string key = GetKeyFromMessage(msg.Content, Log);
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
            string key = GetKeyFromMessage(msg.Content, Log);
            if (key == null) {
                key = StringUtils.GetFileNameFromFilePathOrUrl(attachment.Filename);
                key = StringUtils.RemoveExtension(key);
            }

            await AddEmojiFromUrl(((SocketGuildChannel) msg.Channel).Guild, key, attachment.Url);

            return true;
        }

        public static string GetKeyFromMessage(string message, Action<Core.LogLevel, string> Log) {
            Func<Core.LogLevel, string, Task> l = (level, s) => {
                Log(level, s);
                return Task.CompletedTask;
            };

            return GetKeyFromMessage(message, l);
        }

        public static string GetKeyFromMessage(string message, Func<Core.LogLevel, string, Task> Log) {
            message = message.Replace("emoji", "");
            message = message.Replace("add", "");
            message = message.Replace("process", "");
            message = message.Replace("migrate", "");
            message = message.Replace("reply", "");
            message = StringUtils.RemoveCrap(message);

            Regex formatRx = new(@"[a-zA-Z0-9_]", RegexOptions.Compiled);

            string[] segs = message.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            List<string> candidates = segs.Select(StringUtils.GetFileNameFromFilePathOrUrl)
                                          .Where(seg => seg.Length <= 32).Where(seg => formatRx.IsMatch(seg)).ToList();

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

            string key = GetKeyFromMessage(msg.Content, Log) ?? emote.Name;
            AddEmojiFromUrl(((SocketGuildChannel) msg.Channel).Guild, key, emote.Url);
            return true;
        }

        private async Task AddEmojiFromUrl(IGuild guild, string emoteKey, string url) {
            string filePath = DownloadHelper.DownloadFile(url, emoteKey);
            await AddEmojiFromFile(guild, emoteKey, filePath);
        }

        private async Task AddEmojiFromFile(IGuild guild, string key, string file) {
            string fullPath = Path.GetFullPath(file);
            Image img = new(fullPath);

            await Log(Core.LogLevel.DEBUG, $"Adding emoji {key}. [File: {fullPath}]");
            GuildEmote task = await guild.CreateEmoteAsync(key, img);

            if (task == null) await Log(Core.LogLevel.ERROR, $"Failed to add emoji with key {key}");
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