using System;
using System.Data.SQLite;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core.Model {
    public class Suggestion : IJsonable, ISQLEntity {
        public Suggestion() {
            SuggestionId = -1;
            InternalStatus = (int) VoteStatus.Pending;
        }

        /// Primary Key
        public long SuggestionId;

        /// The originating message Id
        public ulong MessageId;

        /// The message Id of the original reply
        public ulong InitialReplyId;

        public int InternalStatus;

        public VoteStatus Status {
            get => (VoteStatus) InternalStatus;
            set => InternalStatus = (int) value;
        }

        private IUserMessage Message;
        private IUserMessage InitialReply;

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

        // public static void Register(BsonMapper mapper) {
        //     mapper.RegisterType<VoteStatus>(
        //         status => (int) status,
        //         val => (VoteStatus) val.AsInt32
        //     );
        //     mapper.Entity<Suggestion>()
        //           .Field(x => x.MessageId, "message_id")
        //           .Field(x => x.InitialReplyId, "reply_id")
        //           .Field(x => x.InternalStatus, "status");
        // }

        public string[] InsertKeys() {
            return new[] {"messageID", "replyID", "status"};
        }

        public void InsertArgs(SQLiteCommand cmd) {
            cmd.Parameters.AddWithValue("@messageID", MessageId);
            cmd.Parameters.AddWithValue("@replyID", InitialReplyId);
            cmd.Parameters.AddWithValue("@status", InternalStatus);
        }

        public string[] UpdateKeys() {
            return new[] {"status"};
        }

        public void UpdateArgs(SQLiteCommand cmd) {
            cmd.Parameters.AddWithValue("@status", InternalStatus);
        }

        public long GetId() {
            return SuggestionId;
        }
    }
}