using System;
using System.Data.SQLite;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using WordSearchBot.Core.Data.ORM;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core.Model {
    public class Suggestion : IJsonable, ISQLEntity {
        public Suggestion() {
            SuggestionId = -1;
            InternalStatus = (int) VoteStatus.Pending;
        }

        /// Primary Key
        [PrimaryKey]
        public long SuggestionId;

        /// The originating message Id
        [Field("messageID")]
        public ulong MessageId;

        /// The message Id of the original reply
        [Field("replyID")]
        public ulong InitialReplyId;

        [Field("status")]
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

        public string[] InsertKeys() {
            return new[] {"messageID", "replyID", "status"};
        }

        public string[] UpdateKeys() {
            return new[] {"status"};
        }

        private Reflect<ISQLEntity> reflector;
        public Reflect<ISQLEntity> GetReflector() {
            return reflector ??= new Reflect<ISQLEntity>(this);
        }

    }
}