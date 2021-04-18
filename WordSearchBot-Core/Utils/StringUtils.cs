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

    }
}