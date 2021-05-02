using Discord;
using Discord.WebSocket;

namespace WordSearchBot.Core.Web.Mixin {
    public interface IDiscordActor {
        public Core GetDiscord();
    }

    public static class DiscordActorExtension {
        public static IMessage GetMessageInChannel(this IDiscordActor obj, ulong channelId, ulong msgId) {
            Core discord = obj.GetDiscord();
            SocketTextChannel channel = discord.GetContext().Guild.GetTextChannel(channelId);
            return channel.GetMessageAsync(msgId).GetAwaiter().GetResult();
        }
    }

}