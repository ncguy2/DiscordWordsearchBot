using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace WordSearchBot.Core.Utils {
    public static class EmojiSet {

        private static List<SingleEmoji> emojis;

        private static void Initalise() {
            if (emojis != null)
                return;

            Assembly a = Assembly.GetEntryAssembly();
            using Stream stream = a?.GetManifestResourceStream("WordSearchBot_Core.Utils.Emoji.json");
            using StreamReader reader = new (stream!);

            emojis = JsonConvert.DeserializeObject<List<SingleEmoji>>(reader.ReadToEnd());
        }

        public static SingleEmoji getEmoji(string key) {
            Initalise();
            return emojis.FirstOrDefault(x => x.matches(key));
        }

        public static bool getEmoji(string key, out SingleEmoji emoji) {
            emoji = getEmoji(key);
            return emoji != null;
        }

        public static IEnumerable<SingleEmoji> GetEmojis() {
            Initalise();
            return emojis;
        }

        public static void ForEach(Action<SingleEmoji> task) {
            foreach (SingleEmoji singleEmoji in GetEmojis())
                task(singleEmoji);
        }

        public static int CountEmojis() {
            Initalise();
            return emojis.Count;
        }
    }
}