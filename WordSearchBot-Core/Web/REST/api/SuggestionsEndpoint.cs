using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using WordSearchBot.Core.Data.Facade;
using WordSearchBot.Core.Model;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core.Web.REST.api {
    public class SuggestionsEndpoint : RESTEndpoint, IDataEndpoint {
        private readonly Suggestions suggestions;

        public SuggestionsEndpoint() {
            suggestions = new Suggestions();
        }

        public DataPayload DoWork(RequestContext context) {
            SocketTextChannel c = context.discord.GetContext().GetChannelByName<SocketTextChannel>("suggestions");

            Predicate<Suggestion> filter;

            if (context.arguments.ContainsKey("status")) {
                bool tryParse = Enum.TryParse(context.arguments["status"], out VoteStatus s);
                if (!tryParse) {
                    VoteStatus[] values = Enum.GetValues<VoteStatus>();
                    throw new Exception(
                        $"Unable to parse {context.arguments["status"]} as a valid status. Must be one of [{string.Join(", ", values.Select(Enum.GetName))}]");
                }
                filter = x => x.InternalStatus == (int) s;
            } else
                filter = x => true;

            LightweightSuggestion[] sugs = suggestions.Get(filter).Select(x => new LightweightSuggestion {
                SuggestionId = x.SuggestionId.ToString(),
                Status = Enum.GetName(x.Status),
                MessageId = new LightweightSuggestionMessage(c.GetMessageAsync(x.MessageId).Result as IUserMessage),
                ReplyId = new LightweightMessage(c.GetMessageAsync(x.InitialReplyId).Result)
            }).ToArray();

            string json = JsonConvert.SerializeObject(sugs);

            byte[] bytes = Encoding.UTF8.GetBytes(json);

            DataPayload payload = new(bytes);
            payload.StandardHeaders.Add(HttpResponseHeader.ContentType, "application/json");

            return payload;
        }

        public override string GetPath() {
            return "suggestions";
        }

        public override void SubEndpoints(Endpoints endpoints) {
            endpoints.Register<SuggestionsGetEndpoint>();
        }

        protected struct LightweightMessage {
            public ulong Id;
            public ulong AuthorId;
            public string Timestamp;
            public string Content;
            public string JumpUrl;

            public LightweightMessage(IMessage message) {
                Id = message.Id;
                AuthorId = message.Author.Id;
                Timestamp = message.Timestamp.ToString("yy-MM-dd | hh:mm:ss");
                Content = message.Content;
                JumpUrl = message.GetJumpUrl();
            }
        }

        protected struct LightweightSuggestionMessage {
            public ulong Id;
            public ulong AuthorId;
            public string Timestamp;
            public string Content;
            public string JumpUrl;
            public string emoji;

            public LightweightSuggestionMessage(IUserMessage message) {
                Id = message.Id;
                AuthorId = message.Author.Id;
                Timestamp = message.Timestamp.ToString("yy-MM-dd | hh:mm:ss");
                Content = message.Content;
                JumpUrl = message.GetJumpUrl();

                EmojiModule.RequestType type = EmojiModule.DetermineRequestType(message);
                switch (type) {
                    case EmojiModule.RequestType.Attachment:
                        emoji = message.Attachments.First().ProxyUrl;
                        break;
                    case EmojiModule.RequestType.Embed:
                        emoji = LinkFinder.GetUrlsFromString(Content).First();
                        break;
                    case EmojiModule.RequestType.Emoji:
                        ITag? tag = message.Tags.FirstOrDefault(x => x.Type == TagType.Emoji);
                        Tag<Emote> emoteTag = tag as Tag<Emote>;
                        Emote emote = emoteTag.Value;
                        emoji = emote.Url;
                        break;
                    case EmojiModule.RequestType.None:
                    default:
                        emoji = null;
                        break;
                }
            }
        }

        protected struct LightweightSuggestion {
            public string SuggestionId;
            public LightweightSuggestionMessage MessageId;
            public LightweightMessage ReplyId;
            public string Status;
        }
    }
}