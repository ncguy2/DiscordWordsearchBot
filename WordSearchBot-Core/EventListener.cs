using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WordSearchBot.Core {
    public class EventListener<T> where T : class {
        private readonly List<EventListenerFunc<T>> Funcs = new();

        public EventListenerFunc<T> Make() {
            EventListenerFunc<T> eventListenerFunc = new(this);
            Funcs.Add(eventListenerFunc);
            return eventListenerFunc;
        }

        public Func<T, Task> ToFunc() {
            return async t => {
                await Task.Run(async () => {
                    EventObject<T> evt = new(t);
                    foreach (EventListenerFunc<T> func in Funcs) {
                        try {
                            await func.Execute(evt);
                        } catch (ModuleException e) {
                            await e.throwingModule.Log(Core.LogLevel.ERROR, e.Message);
                            evt.Consume();
                        }
                        if (evt.Consumed())
                            return;
                    }
                });
            };
        }

        public Func<U, Task> ToCastedFunc<U>(Func<U, T> cast = null) {
            Func<T,Task> func = ToFunc();
            cast ??= u => u as T;

            return async u => {
                await func(cast(u));
            };
        }
    }

    public class EventObject<T> {
        public readonly T Object;
        private bool consumed;

        public EventObject(T obj) {
            Object = obj;
            consumed = false;
        }

        public void Consume() {
            consumed = true;
        }

        public bool Consumed() {
            return consumed;
        }
    }

    public class EventListenerFunc<T> where T : class {
        private readonly EventListener<T> _parentListener;
        private readonly List<Func<T, bool>> Predicates = new();
        private readonly List<Func<T, Task>> Tasks = new();

        public EventListenerFunc(EventListener<T> parentListener) {
            _parentListener = parentListener;
        }

        public EventListenerFunc<T> AddPredicate(Func<T, bool> predicate) {
            Predicates.Add(predicate);
            return this;
        }

        public EventListenerFunc<T> AddTask(Func<T, Task> task) {
            Tasks.Add(task);
            return this;
        }

        public bool Test(T obj) {
            return Predicates.All(predicate => predicate(obj));
        }

        public async Task Run(T obj) {
            foreach (Func<T, Task> task in Tasks)
                await task(obj);
        }

        public async Task Execute(EventObject<T> t) {
            if (!Test(t.Object))
                return;

            t.Consume();
            await Run(t.Object);
        }

        public EventListener<T> Finish() {
            return _parentListener;
        }
    }
}