using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using WordSearchBot.Core.Model;

namespace WordSearchBot.Core.Data.Facade {
    public class Facade<T> where T : ISQLEntity {

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

        public IEnumerable<T> Get(Predicate<T> predicate) {
            return Storage.Get<T>(predicate);
        }

        public T Get(string id) {
            return Storage.GetById<T>(long.Parse(id));
        }

    }
}