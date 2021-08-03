using System;
using System.Collections.Generic;
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
            "Please refrain from posting spoilers in the future",
            "Please refrain from posting spoilers in the future",
            "Please refrain from posting spoilers in the future",
            "Please refrain from posting spoilers in the future, especially if you're a fan of your kneecaps",
            "If you like your toes, don't post any more spoilers"
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
            eb.WithAuthor(msg.Author).WithDescription($"Spoilers: ||{msg.Content}||");

            string url = null;

            if (msg.Attachments.Count > 0) {
                // TODO Assumption that user messages can only have up to a single attachment
                IAttachment attachment = msg.Attachments.ToArray()[0];
                url = attachment.Url;
            }

            List<string> urls = LinkFinder.GetUrlsFromString(msg.Content);
            if (url == null && urls.Count > 0)
                url = urls[0];

            if (url != null) {
                DownloadHelper.DownloadTempFileAsync(url, async info => {

                    string content = null;
                    if(urls.Count > 0) {
                        int startIdx = url == urls[0] ? 1 : 0;
                        for (int i = startIdx; i < urls.Count; i++) {
                            content += $"||{url}||\n";
                        }
                    }

                    RestUserMessage newMsg = await channel.SendFileAsync(info.FullName, text: content, embed: eb.Build(), isSpoiler: true);
                    await SendPersonalMessageAboutSpoiler(msg, newMsg.GetJumpUrl(), channel, isFile: true);
                    await DeleteMessage(msg);
                });

                return;
            }

            RestUserMessage newMsg = await channel.SendMessageAsync(embed: eb.Build());

            await SendPersonalMessageAboutSpoiler(msg, newMsg.GetJumpUrl(), channel);
            await DeleteMessage(msg);
        }

        private async Task SendPersonalMessageAboutSpoiler(IUserMessage msg, string jumpUrl, IChannel channel, bool isFile = false) {
            EmbedBuilder eb = EmbedUtils.ExternalEmbed();
            eb.WithTitle("No Spoilers!").WithUrl(jumpUrl);
            eb.WithColor(149, 13, 149);

            string desc = isFile ? "You uploaded a file that someone thought contains a spoiler, so I've reposted it appropriately" : "Someone decided that a message of yours to contain a spoiler, so I've hidden it";

            eb.WithDescription($"{desc}\n{CollectionUtils.SelectRandom(Sublines)}");

            eb.AddField("Server", (channel as ITextChannel)?.Guild.Name, inline: true);
            eb.AddField("Channel", channel.Name, inline: true);
            string content = msg.Content;
            if (string.IsNullOrWhiteSpace(content))
                content = "[No Content]";
            eb.AddField("The message in question", content, inline: false);

            await msg.Author.SendMessageAsync(embed: eb.Build());
        }

        private async Task DeleteMessage(IUserMessage msg) {
            await msg.DeleteAsync();
            // await msg.ModifySuppressionAsync(true);
            // await msg.AddReactionAsync(EmojiHelper.getEmoji("wastebasket").asEmote());
        }

    }
}