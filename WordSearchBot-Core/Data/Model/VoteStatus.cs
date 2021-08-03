using System;
using Discord;

namespace WordSearchBot.Core.Model {
    [Flags]
    public enum VoteStatus {

        // Bits 0 and 1
        Pending = 0b0000,
        Passed = 0b0001,
        Vetoed = 0b0010,
        Erroneous = 0b0011,

        // Bits 2 and 3
        Sticker = 0b0100,
    }

    public static class VoteMasks {
        public static readonly int Status = 0b0011;
        public static readonly int Category = 0b1100;

        public static VoteStatus GetStatus(VoteStatus statusValue) {
            return (VoteStatus) (((int)statusValue) & Status);
        }

        public static VoteStatus GetCategory(VoteStatus statusValue) {
            return (VoteStatus) (((int)statusValue) & Category);
        }
    }
}