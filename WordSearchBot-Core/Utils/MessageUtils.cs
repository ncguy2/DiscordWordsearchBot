using System;
using System.Threading.Tasks;
using Discord;

namespace WordSearchBot.Core.Utils {
    public static class MessageUtils {
        public static async Task LongTaskMessage(IUserMessage msg, Embed initialMessage,
                                                 Func<IUserMessage, LongTaskMessageReturn> callback) {
            IUserMessage userMessage = await msg.ReplyAsync(embed: initialMessage);
            LongTaskMessageReturn embed = callback(msg);
            await userMessage.ModifyAsync(p => {
                if (embed.isEmbed)
                    p.Embed = embed.embedContent;
                else
                    p.Content = embed.strContent;
            });
        }

        public static async Task LongTaskMessage(IUserMessage msg, string initialMessage,
                                                 Func<IUserMessage, LongTaskMessageReturn> callback) {
            IUserMessage userMessage = await msg.ReplyAsync(initialMessage);
            LongTaskMessageReturn embed = callback(msg);
            await userMessage.ModifyAsync(p => {
                if (embed.isEmbed)
                    p.Embed = embed.embedContent;
                else
                    p.Content = embed.strContent;
            });
        }
    }

    public struct LongTaskMessageReturn {
        public string strContent;
        public Embed embedContent;

        public bool isEmbed;

        private LongTaskMessageReturn(string strContent, Embed embedContent) {
            this.strContent = strContent;
            this.embedContent = embedContent;
            isEmbed = embedContent != null;
        }

        public LongTaskMessageReturn(string strContent) : this(strContent, null) { }
        public LongTaskMessageReturn(Embed embed) : this(null, embed) { }
    }
}