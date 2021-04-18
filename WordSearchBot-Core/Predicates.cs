using System;
using System.Linq;
using Discord;
using Discord.WebSocket;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core {
    public static class Predicates {

        public static Func<IUserMessage, bool> FilterOnBotMessage() {
            return t => !(t.Author.IsBot || t.Author.IsWebhook);
        }

        public static Func<IUserMessage, bool> FilterOnMention(SocketUser user) {
            return t => t.MentionedUserIds.Contains(user.Id);
        }

        public static Func<IUserMessage, bool> FilterOnChannelId(ulong channelId) {
            return t => t.Channel.Id == channelId;
        }

        public static Func<IUserMessage, bool> FilterOnCommand(string cmd) {
            return t => StringUtils.RemoveCrap(t.Content)
                                   .Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[
                                       0] == cmd;
        }

        public static Func<IUserMessage, bool> FilterOnCommandPattern(params string[] cmds) {
            return t => {
                string[] words = StringUtils.RemoveCrap(t.Content)
                                            .Split(
                                                " ",
                                                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (words.Length < cmds.Length)
                    return false;

                return !cmds.Where((t1, i) => t1 != words[i]).Any();
            };
        }
    }
}