using System;
using System.Threading.Tasks;

namespace WordSearchBot.Core.Utils {
    public static class TaskUtils {

        public static void Run(Func<Task> task) {
            Task.Run(() => {
                try {
                    task().GetAwaiter().GetResult();
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.ToString());
                }
            });
        }

    }
}