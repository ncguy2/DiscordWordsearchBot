using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using WordSearchBot.Core.Data.Facade;
using WordSearchBot.Core.Model;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core
{
    public class StickerModule : Module
    {
        protected Suggestions suggestions;
        protected readonly int VoteThreshold = ConfigKeys.Emoji.VOTE_THRESHOLD.Get();
        public override string DisplayName()
        {
            return "Sticker";
        }

        public override string InternalName()
        {
            return "sticker";
        }

        public override void RegisterEvents(DiscordContext context)
        {
            suggestions = new Suggestions();
        }

        private async Task SuggestSticker(IUserMessage msg)
        {
            if (IsMessageAlreadyHandled(msg)) {
                msg.AddReactionAsync(EmojiHelper.getEmote(AssignedCore.GetClient(), "confused").asEmote());
                return;
            }

            EmojiModule.RequestType requestType = EmojiModule.DetermineRequestType(msg, Log);
            if (requestType == EmojiModule.RequestType.None)
                return;

            EmojiModule.ValidityStatus validity = await InspectSticker(msg);

            if (validity == EmojiModule.ValidityStatus.Valid) {
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

            replyMsg += EmojiModule.GetErrorMessages(validity);

            msg.ReplyAsync(replyMsg);
        }
        
        private bool IsMessageAlreadyHandled(IUserMessage msg) {
            return suggestions.Contains(msg.Id);
        }

        private async Task<EmojiModule.ValidityStatus> InspectSticker(IUserMessage msg)
        {
            ITextChannel textChannel = msg.Channel as ITextChannel;
            if (textChannel == null)
                return EmojiModule.ValidityStatus.Unknown_Error;
            IGuild guild = textChannel.Guild;
            int emoteLimit = GetStickerLimit(guild);

            EmojiModule.RequestType type = EmojiModule.DetermineRequestType(msg, Log);

            if (type == EmojiModule.RequestType.None && msg.ReferencedMessage != null) {
                msg = msg.ReferencedMessage;
                type = EmojiModule.DetermineRequestType(msg, Log);
            }

            string key = EmojiModule.GetKeyFromMessage(msg.Content, Log);

            EmojiModule.ValidityStatus flags = 0;

            if (key.Length < 2)
                flags |= EmojiModule.ValidityStatus.Name_Too_Short;

            if(new Regex("[^a-zA-Z0-9_]").IsMatch(key))
                flags |= EmojiModule.ValidityStatus.Name_Not_Alphanumeric;

            //TODO Change Emotes to Stickers once it's exposed
            if (guild.Emotes.Count == emoteLimit)
                flags |= EmojiModule.ValidityStatus.Insufficient_Slots;

            const int maxFileSize = 512000; // 500KB
            switch (type) {
                case EmojiModule.RequestType.Attachment:
                    IAttachment attachment = msg.Attachments.ToArray()[0];
                    if (attachment.Size > maxFileSize)
                        flags |= EmojiModule.ValidityStatus.File_Size_Too_Big;
                    break;
                case EmojiModule.RequestType.Embed:
                    string url = LinkFinder.GetUrlsFromString(msg.Content)[0];

                    try {
                        DownloadHelper.DownloadTempFile(url, file => {
                            if (file.Length > maxFileSize)
                                flags |= EmojiModule.ValidityStatus.File_Size_Too_Big;
                        });
                    } catch (WebException we) {
                        msg.ReplyAsync(we.Message);
                        return EmojiModule.ValidityStatus.Silent_Error;
                    }

                    break;
            }

            return flags;
        }
        
        public int GetStickerLimit(IGuild guild) {
            return guild.PremiumTier switch {
                PremiumTier.None => 0,
                PremiumTier.Tier1 => 15,
                PremiumTier.Tier2 => 30,
                PremiumTier.Tier3 => 60,
                _ => 0
            };
        }
        
    }
}