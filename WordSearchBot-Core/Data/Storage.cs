using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using LiteDB;

namespace WordSearchBot.Core.Data {
    public static class Storage {

        private static readonly string DB_FILE = ConfigKeys.Cache.DB_FILE.Get();
        private static bool dbChecked = false;

        private static void check() {
            if (dbChecked)
                return;

            if (DB_FILE.Length == 0)
                throw new Exception($"Database file: \"{DB_FILE}\" is not specified");

            dbChecked = true;
        }

        public static T GetById<T>(BsonValue id) {
            check();
            using LiteDatabase db = new(DB_FILE);
            ILiteCollection<T> col = db.GetCollection<T>();

            return col.FindById(id);
        }

        public static T GetFirst<T>(Expression<Func<T, bool>> predicate) {
            check();
            using LiteDatabase db = new(DB_FILE);
            ILiteCollection<T> col = db.GetCollection<T>();

            return col.FindOne(predicate);
        }

        public static IEnumerable<T> Get<T>(Expression<Func<T, bool>> predicate) {
            check();
            using LiteDatabase db = new(DB_FILE);
            ILiteCollection<T> col = db.GetCollection<T>();

            return col.Find(predicate).ToList();
        }

        public static IEnumerable<T> GetAll<T>() {
            check();
            using LiteDatabase db = new(DB_FILE);
            ILiteCollection<T> col = db.GetCollection<T>();

            return col.FindAll().ToList();
        }

        public static void Insert<T>(T obj) {
            check();
            using LiteDatabase db = new(DB_FILE);
            ILiteCollection<T> col = db.GetCollection<T>();

            col.Insert(obj);
        }

        public static bool Update<T>(T obj) {
            check();
            using LiteDatabase db = new(DB_FILE);
            ILiteCollection<T> col = db.GetCollection<T>();

            return col.Update(obj);
        }

        public static void TryInsertOrUpdate<T>(T obj) {
            check();
            using LiteDatabase db = new(DB_FILE);
            ILiteCollection<T> col = db.GetCollection<T>();

            if (!col.Update(obj))
                col.Insert(obj);
        }


        public static class Utils {
            public static Expression<Func<T, bool>> ConvertPredicate<T>(Predicate<T> predicate) {
                ParameterExpression p0 = Expression.Parameter(typeof(T));
                return Expression.Lambda<Func<T, bool>>(Expression.Call(predicate.Method, p0), p0);
            }
        }

    }
}