using System;
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

        public bool ContainsAndIs(ulong msgId, Predicate<Suggestion> predicate) {
            Suggestion msg = GetFromMessageId(msgId);
            return msg != null && predicate(msg);
        }

        public Suggestion Create(IUserMessage msg) {
            return new() {MessageId = msg.Id};
        }
    }
}