using System;

namespace WordSearchBot.Core.Utils {
    public static class CollectionUtils {

        private static Random random = new Random();

        public static T SelectRandom<T>(T[] array) {
            return array[random.Next(array.Length)];
        }

    }
}