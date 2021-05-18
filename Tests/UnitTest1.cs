using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Tests {
    public class Tests {

        [Test]
        public void Test1() {
            Regex regex = new Regex("[^a-zA-Z0-9_]");
            Console.WriteLine(regex.IsMatch("asdffsdf"));
            Console.WriteLine(regex.IsMatch("asdf:>fsdf"));
        }
    }
}