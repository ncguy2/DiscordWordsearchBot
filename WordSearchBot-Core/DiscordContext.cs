using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace WordSearchBot.Core {
    public class DiscordContext {
        public DiscordSocketClient Client;
        public SocketGuild Guild;
        public EventListener<IUserMessage> MessageListener;

        protected Dictionary<string, SocketChannel> ChannelCache = new ();

        public SocketChannel GetChannelByName(string name) {
            if (ChannelCache.ContainsKey(name))
                return ChannelCache[name];

            foreach (SocketGuildChannel socketGuildChannel in Guild.Channels) {
                if (socketGuildChannel.Name != name)
                    continue;
                ChannelCache.Add(name, socketGuildChannel);
                return socketGuildChannel;
            }

            return null;
        }

    }
}