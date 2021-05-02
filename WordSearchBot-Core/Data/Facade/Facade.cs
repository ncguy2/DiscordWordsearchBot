using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WordSearchBot.Core.Data.Facade {
    public class Facade<T> {

        public bool Update(T obj) {
            return Storage.Update(obj);
        }

        public void Insert(T obj) {
            Storage.Insert(obj);
        }

        public void TryInsertOrUpdate(T obj) {
            Storage.TryInsertOrUpdate(obj);
        }

        public IEnumerable<T> Get() {
            return Storage.GetAll<T>();
        }

        public IEnumerable<T> Get(Expression<Func<T, bool>> predicate) {
            return Storage.Get<T>(predicate);
        }

        public T Get(string id) {
            return Storage.GetById<T>(id);
        }

    }
}