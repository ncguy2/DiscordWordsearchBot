using System.Linq.Expressions;
using Discord;
using LiteDB;
using WordSearchBot.Core.Model;

namespace WordSearchBot.Core.Data.Facade {
    public class Suggestions : Facade<Suggestion> {

        public Suggestion GetFromMessageId(ulong msgId) {
            return Storage.GetFirst<Suggestion>(x => x.MessageId == msgId);
        }

        public bool Contains(ulong msgId) {
            return GetFromMessageId(msgId) != null;
        }

        public Suggestion Create(IUserMessage msg) {
            return new() {MessageId = msg.Id};
        }
    }
}