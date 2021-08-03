using System;

namespace WordSearchBot.Core.Utils {
    public static class CollectionUtils {

        private static Random random = new Random();

        public static T SelectRandom<T>(T[] array) {
            int index = random.Next(array.Length);
            return array[index];
        }

    }
}