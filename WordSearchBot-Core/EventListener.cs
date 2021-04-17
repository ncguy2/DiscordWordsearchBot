using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WordSearchBot.Core {
    public class EventListener<T> {
        private List<Func<T, bool>> Predicates = new();
        private List<Func<T, Task>> Tasks = new();

        public EventListener<T> AddPredicate(Func<T, bool> predicate) {
            Predicates.Add(predicate);
            return this;
        }

        public EventListener<T> AddTask(Func<T, Task> task) {
            Tasks.Add(task);
            return this;
        }

        public bool Test(T obj) {
            return Predicates.All(predicate => predicate(obj));
        }

        public async Task Run(T obj) {
            foreach (Func<T,Task> task in Tasks)
                await task(obj);
        }

        public Func<T, Task> ToFunc() {
            return t => Test(t) ? Run(t) : Task.CompletedTask;
        }

    }
}