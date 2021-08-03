using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core.Modules {
    public class SpoilerModule : Module {

        const long SPOILER_ID = 872099412795588619;

        private static readonly string[] Sublines = {
            "",
            "Please refrain from posting spoilers in the future",
            "Please refrain from posting spoilers in the future, especially if you're a fan of your kneecaps"
        };

        public override string DisplayName() {
            return "Spoilers";
        }

        public override string InternalName() {
            return "spoiler_hider";
        }

        public override void RegisterEvents(DiscordContext context) {
            context.Client.ReactionAdded += (msg, channel, reaction) => {
                TaskUtils.Run(() => TryMarkMessageAsSpoiler(msg, channel, reaction));
                return Task.CompletedTask;
            };
        }

        private bool IsReactionSpoilerTag(IReaction reaction) {
            return reaction.Emote is Emote { Id: SPOILER_ID } || reaction.Emote.Name.ToLower().Contains("spoiler");
        }

        private async Task TryMarkMessageAsSpoiler(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, IReaction reaction) {
            if (!IsReactionSpoilerTag(reaction))
                return;

            IUserMessage userMsg = await msg.GetOrDownloadAsync();

            if (userMsg.Author.IsBot || userMsg.Author.IsWebhook)
                return;

            await MarkMessageAsSpoiler(userMsg, channel);
        }

        private async Task MarkMessageAsSpoiler(IUserMessage msg, ISocketMessageChannel channel) {

            EmbedBuilder eb = new();
            eb.WithAuthor(msg.Author).WithDescription(msg.Content);

            string url = null;

            if (msg.Attachments.Count > 0) {
                // TODO Assumption that user messages can only have up to a single attachment
                IAttachment attachment = msg.Attachments.ToArray()[0];
                url = attachment.Url;
            }

            if (url != null) {
                DownloadHelper.DownloadTempFileAsync(url, async info => {
                    await channel.SendFileAsync(info.FullName, embed: eb.Build(), isSpoiler: true);
                    await msg.DeleteAsync();
                });

                return;
            }

            RestUserMessage newMsg = await channel.SendMessageAsync(embed: eb.Build());


            eb = new EmbedBuilder();
            eb.WithUrl(newMsg.GetJumpUrl());
            eb.WithColor(149, 13, 149);
            eb.WithDescription("Someone deemed a message of yours to contain a spoiler, so I've hidden it :)\n" +
                               CollectionUtils.SelectRandom(Sublines));

            await msg.DeleteAsync();
        }

    }
}