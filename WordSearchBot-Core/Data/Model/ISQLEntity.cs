using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using WordSearchBot.Core.Data;
using WordSearchBot.Core.Data.ORM;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core.Model {
    public interface ISQLEntity {
        string[] InsertKeys();
        string[] UpdateKeys();
        Reflect<ISQLEntity> GetReflector();
    }

    public static class ISQLEntityExtension {
        public static bool IsStored(this ISQLEntity entity) {
            return entity.GetId() >= 0;
        }

        public static long GetId(this ISQLEntity entity) {
            FieldInfo idField = entity.GetReflector().GetFieldWithAttribute<PrimaryKeyAttribute>();
            object value = idField.GetValue(entity);
            if (value == null)
                return -1;
            return (long)value;
        }

        public static string GetValueAsString(this ISQLEntity entity, string label) {
            FieldInfo[] fields = entity.GetReflector().GetFieldsWithAttribute<FieldAttribute>();
            FieldInfo field = fields
                              .Select(x => new Tuple<FieldInfo, FieldAttribute>(
                                          x, x.GetCustomAttribute<FieldAttribute>()))
                              .First(t => t.Item2.GetName(t.Item1) == label).Item1;
            return field.GetValue(entity)?.ToString();
        }

        public static void Args(this ISQLEntity entity, SQLiteCommand cmd, IEnumerable<string> keys) {
            foreach (string key in keys)
                cmd.Parameters.AddWithValue("@" + key, entity.GetValueAsString(key));
        }

        public static bool Insert<T>(this T entity) where T : ISQLEntity {
            return Storage.Insert(entity);
        }

        public static bool Update<T>(this T entity) where T : ISQLEntity {
            return Storage.Update(entity);
        }

        public static bool TryInsertOrUpdate<T>(this T entity) where T : ISQLEntity {
            return Storage.TryInsertOrUpdate(entity);
        }
    }
}