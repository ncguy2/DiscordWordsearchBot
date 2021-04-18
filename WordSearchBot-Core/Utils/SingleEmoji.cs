using System.Linq;

namespace WordSearchBot.Core.Utils {
    public class SingleEmoji {
        public string emoji;
        public string description;
        public string category;
        public string[] aliases;
        public string unicode_version;
        public string ios_version;

        public bool matches(string key) {
            return aliases.Contains(key);
        }
    }
}