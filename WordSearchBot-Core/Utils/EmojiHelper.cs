using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace WordSearchBot.Core.Utils {

    public class EmojiItem {
        public enum Type {
            Emote,
            Emoji
        }

        public readonly Type type;

        public EmojiItem(Emote emote) {
            type = Type.Emote;
            this.emote = emote;
        }

        public EmojiItem(SingleEmoji emoji) {
            type = Type.Emoji;
            this.emoji = emoji;
        }

        private readonly Emote emote;
        private readonly SingleEmoji emoji;

        public Emote getEmote() {
            return emote;
        }

        public SingleEmoji getEmoji() {
            return emoji;
        }

        public string embedString() {
            return type switch {
                Type.Emoji => getEmoji().emoji,
                Type.Emote => getEmote().ToString(),
                _ => "ERROR"
            };
        }

        public override string ToString() {
            return embedString();
        }

        public IEmote asEmote() {
            return type switch {
                Type.Emoji => new Emoji(getEmoji().emoji),
                Type.Emote => getEmote(),
                _ => null
            };
        }

    }

    public static class EmojiHelper {

        private static Dictionary<string, EmojiItem> emoteCache = new ();

        private static bool cache(string key, out EmojiItem val) {
            if (emoteCache.ContainsKey(key)) {
                val = emoteCache[key];
                return true;
            }

            val = null;
            return false;
        }

        private static EmojiItem cache(string key, EmojiItem val) {
            if(!emoteCache.ContainsKey(key))
                emoteCache.Add(key, val);
            return val;
        }

        public static string formatText(DiscordSocketClient client, string text) {
            string[] words = text.Split(" ");

            for (int i = 0; i < words.Length; i++) {
                string word = words[i];
                if(word == null || word.Length < 2)
                    continue;

                if (!word.StartsWith(":") || !word.EndsWith(":"))
                    continue;

                EmojiItem emote = getEmote(client, word);
                words[i] = (emote != null ? emote.embedString() : word);
            }

            return string.Join(" ", words);
        }

        public static Emote getEmote(string emoteKey) {
            return Emote.TryParse(emoteKey, out Emote res) ? res : null;
        }

        public static EmojiItem getEmoji(string emoteKey) {
            emoteKey = emoteKey.Trim(':');

            if (EmojiSet.getEmoji(emoteKey, out SingleEmoji emoji))
                return cache(emoteKey, new EmojiItem(emoji));

            if (cache(emoteKey, out EmojiItem e))
                return e;

            if (Emote.TryParse(emoteKey, out Emote emote))
                return cache(emoteKey, new EmojiItem(emote));

            return null;
        }

        public static EmojiItem getEmote(DiscordSocketClient client, string emoteKey) {
            emoteKey = emoteKey.Trim(':');

            if (EmojiSet.getEmoji(emoteKey, out SingleEmoji emoji))
                return cache(emoteKey, new EmojiItem(emoji));

            if (cache(emoteKey, out EmojiItem e))
                return e;

            if (Emote.TryParse(emoteKey, out Emote emote))
                return cache(emoteKey, new EmojiItem(emote));

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (SocketGuild guild in client.Guilds) {
                GuildEmote[] guildEmotes = guild.Emotes.Where(x => x.Name.Equals(emoteKey)).ToArray();
                if (guildEmotes.Length > 0)
                    return cache(emoteKey, new EmojiItem(guildEmotes[0]));
            }

            return null;
        }

    }
}