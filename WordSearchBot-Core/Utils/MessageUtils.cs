using System;
using System.Threading.Tasks;
using Discord;

namespace WordSearchBot.Core.Utils {
    public static class MessageUtils {
        public static void LongTaskMessage(IUserMessage msg, Embed initialMessage,
                                                 Func<IUserMessage, IUserMessage, Task<LongTaskMessageReturn>> callback) {
            Task.Run(async () => {
                IUserMessage userMessage = await msg.ReplyAsync(embed: initialMessage);
                LongTaskMessageReturn embed = await callback(msg, userMessage);
                await userMessage.ModifyAsync(p => {
                    if (embed.embedContent != null)
                        p.Embed = embed.embedContent;
                    p.Content = embed.strContent ?? "";
                });
            });
        }

        public static void LongTaskMessage(IUserMessage msg, string initialMessage,
                                                 Func<IUserMessage, IUserMessage, Task<LongTaskMessageReturn>> callback) {
            Task.Run(async () => {
                IUserMessage userMessage = await msg.ReplyAsync(initialMessage);
                LongTaskMessageReturn embed = await callback(msg, userMessage);
                await userMessage.ModifyAsync(p => {
                    if (embed.embedContent != null)
                        p.Embed = embed.embedContent;
                    p.Content = embed.strContent ?? "";
                });
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