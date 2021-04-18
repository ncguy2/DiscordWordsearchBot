using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WordSearchBot.Core.Utils {

    public static class LinkFinder {
        public static List<string> GetUrlsFromString(string str) {
            return str.Split("\t\n ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                      .Where(s => s.StartsWith("http://") || s.StartsWith("https://")).ToList();
        }
    }
}