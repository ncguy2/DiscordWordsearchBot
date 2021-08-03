using System;
using System.Text.RegularExpressions;
using Discord;
using NUnit.Framework;
using WordSearchBot.Core.Utils;

namespace Tests {
    public class Tests {
        [Test]
        public void Test1() {
            Regex regex = new("[^a-zA-Z0-9_]");
            Console.WriteLine(regex.IsMatch("asdffsdf"));
            Console.WriteLine(regex.IsMatch("asdf:>fsdf"));
        }

        [Test]
        public void Test2() {
            EmojiItem emojiItem = EmojiHelper.getEmoji("wastebasket");
        }
    }
}