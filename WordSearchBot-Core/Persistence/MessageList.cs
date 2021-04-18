using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace WordSearchBot.Core {
    public class MessageList : WatchList<IUserMessage, MessageSerialiser> {
        public MessageList(string backingFile, SocketTextChannel channel) : base(backingFile) {
            ((MessageSerialiser) Serialiser).Set(channel);
        }

        public bool Contains(IMessage msg) {
            return Data.Select(x => x.Id).Contains(msg.Id);
        }
        public bool Contains(ulong msgId) {
            return Data.Select(x => x.Id).Contains(msgId);
        }

        public void Remove(IUserMessage message) {
            Data.RemoveAll(x => x.Id == message.Id);
            Write();
        }
    }

    public class MessageSerialiser : DataSerialiser<IUserMessage> {

        protected SocketTextChannel Channel;

        public void Set(SocketTextChannel channel) {
            Channel = channel;
        }

        public override IUserMessage Deserialize(string str) {
            if (!ulong.TryParse(str, out ulong msgId))
                throw new Exception($"Failed to parse message id from {str}, check serializer function");

            return Channel.GetMessageAsync(msgId).GetAwaiter().GetResult() as IUserMessage;
        }

        public override string Serialize(IUserMessage obj) {
            return obj.Id.ToString();
        }

    }

}