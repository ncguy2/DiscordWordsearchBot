using System;

namespace WordSearchBot.Core.System {
    [AttributeUsage(AttributeTargets.Assembly)]
    public class GitRevisionAttribute : Attribute {

        public GitRevisionAttribute(string hash)
        {
            Hash = hash;
        }

        public string Hash { get; }

    }
}