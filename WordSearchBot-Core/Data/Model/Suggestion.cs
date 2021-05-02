using System;
using Discord;
using Discord.WebSocket;
using LiteDB;

namespace WordSearchBot.Core.Model {
    public class Suggestion {
        public Suggestion() {
            SuggestionId = ObjectId.NewObjectId();
            InternalStatus = (int) VoteStatus.Pending;
        }

        /// Primary Key
        [BsonId]
        public ObjectId SuggestionId;

        /// The originating message Id
        public ulong MessageId;

        /// The message Id of the original reply
        public ulong InitialReplyId;

        public int InternalStatus;

        public VoteStatus Status {
            get => (VoteStatus) InternalStatus;
            set => InternalStatus = (int) value;
        }

        [BsonIgnore] private IUserMessage Message;
        [BsonIgnore] private IUserMessage InitialReply;

        public IUserMessage GetMessage(IMessageChannel channel = null) {
            if (Message == null && channel == null)
                throw new Exception("Channel needs to be specified the first time the Message is requested");

            return Message ??= channel.GetMessageAsync(MessageId).Result as IUserMessage;
        }

        public IUserMessage GetReply(IMessageChannel channel = null) {
            if (InitialReply == null && channel == null)
                throw new Exception("Channel needs to be specified the first time the InitialReply is requested");

            return InitialReply ??= channel.GetMessageAsync(InitialReplyId).Result as IUserMessage;
        }

        public static void Register(BsonMapper mapper) {
            mapper.RegisterType<VoteStatus>(
                status => (int) status,
                val => (VoteStatus) val.AsInt32
            );
            mapper.Entity<Suggestion>()
                  .Field(x => x.MessageId, "message_id")
                  .Field(x => x.InitialReplyId, "reply_id")
                  .Field(x => x.InternalStatus, "status");
        }

    }
}