using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace WordSearchBot.Core.Utils {
    public static class StringUtils {

        public static string RemoveCrap(string message) {
            Regex rx = new(@"<.*?>", RegexOptions.Compiled);
            Regex spaceRx = new(@"\s+", RegexOptions.Compiled);
            message = rx.Replace(message, "");
            message = spaceRx.Replace(message, " ");
            message = message.Trim();
            return message;
        }

        public static string GetFileNameFromFilePathOrUrl(string path) {
            path = path.Replace("\\", "/");
            int lastIndexOf = path.LastIndexOf("/", StringComparison.Ordinal);
            return lastIndexOf < 0 ? path : path.Substring(lastIndexOf + 1);
        }

        public static string RemoveExtension(string name) {
            int lastIndexOf = name.LastIndexOf(".", StringComparison.Ordinal);
            return lastIndexOf < 0 ? name : name.Substring(0, lastIndexOf);
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}