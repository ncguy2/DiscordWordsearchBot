using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using WordSearchBot.Core.Model;

namespace WordSearchBot.Core.Data {
    public static class Storage {

        private static SQLiteConnection connection;
        private static SQLiteConnection DB => GetConnection();

        private static SQLiteConnection GetConnection() {
            if (connection == null) {
                connection = new(DB_FILE);
                connection.Open();
            }

            return connection;
        }



        private static readonly string DB_FILE_STR =
            ConfigKeys.Cache.CACHE_DIR.Get() + "/" + ConfigKeys.Cache.DB_FILE.Get();
        private static readonly string DB_FILE = "URI=file:" + new FileInfo(DB_FILE_STR).FullName;
        private static bool dbChecked = false;

        private static Dictionary<Type, Func<string>> Builders = new();
        private static Dictionary<Type, Func<SQLiteDataReader, dynamic>> Readers = new();

        private static void check() {
            if (dbChecked)
                return;

            Console.WriteLine("DB File: " + DB_FILE);
            SQLiteConnection.CreateFile(ConfigKeys.Cache.DB_FILE.Get());

            if (DB_FILE.Length == 0)
                throw new Exception($"Database file: \"{DB_FILE}\" is not specified");

            Builders.Add(typeof(Suggestion), () => $"CREATE TABLE {nameof(Suggestion)}(" +
                                                   "id INTEGER PRIMARY KEY autoincrement," +
                                                   "messageID INTEGER," +
                                                   "replyID INTEGER," +
                                                   "status INTEGER)");

            AddReader<Suggestion>(reader => {
                Suggestion suggestion = new() {
                    SuggestionId = reader.GetInt64(0),
                    MessageId = (ulong) reader.GetInt64(1),
                    InitialReplyId = (ulong) reader.GetInt64(2),
                    InternalStatus = reader.GetInt32(3)
                };
                return suggestion;
            });

            dbChecked = true;
        }

        private static void AddReader<T>(Func<SQLiteDataReader, dynamic> readerFunc) {
            Readers.Add(typeof(T), readerFunc);
        }

        private static Func<SQLiteDataReader, dynamic> GetReader<T>() {
            return Readers[typeof(T)];
        }

        public static void CheckTable<T>() {
            check();
            string tableName = typeof(T).Name;
            string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name=@table_name;";


            SQLiteCommand cmd = new(DB) {
                CommandText = sql
            };
            cmd.Parameters.AddWithValue("@table_name", tableName);

            SQLiteDataReader sqLiteDataReader = cmd.ExecuteReader();
            if (sqLiteDataReader.HasRows)
                return;

            cmd = new SQLiteCommand(DB) {CommandText = Builders[typeof(T)]()};
            cmd.ExecuteNonQuery();
        }

        public static T GetById<T>(long id) {
            check();
            CheckTable<T>();

            SQLiteCommand cmd = new(DB);
            cmd.CommandText = $"SELECT * FROM {typeof(T).Name} WHERE id=@id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Prepare();
            SQLiteDataReader sqLiteDataReader = cmd.ExecuteReader();

            if (!sqLiteDataReader.Read())
                throw new Exception();
            dynamic byId = GetReader<T>().Invoke(sqLiteDataReader);
            return byId;
        }

        public static T GetFirst<T>(Predicate<T> predicate) {
            check();
            CheckTable<T>();

            SQLiteCommand cmd = new(DB);
            cmd.CommandText = $"SELECT * FROM {typeof(T).Name};";
            cmd.Prepare();
            SQLiteDataReader sqLiteDataReader = cmd.ExecuteReader();

            while (sqLiteDataReader.Read()) {
                dynamic byId = GetReader<T>().Invoke(sqLiteDataReader);
                if(predicate(byId))
                    return byId;
            }

            throw new Exception();
        }

        public static IEnumerable<T> Get<T>(Predicate<T> predicate) {
            check();
            CheckTable<T>();

            SQLiteCommand cmd = new(DB);
            cmd.CommandText = $"SELECT * FROM {typeof(T).Name};";
            cmd.Prepare();
            SQLiteDataReader sqLiteDataReader = cmd.ExecuteReader();

            while (sqLiteDataReader.Read()) {
                dynamic byId = GetReader<T>().Invoke(sqLiteDataReader);
                if(predicate(byId))
                    yield return byId;
            }
        }

        public static IEnumerable<T> GetAll<T>() {
            check();
            CheckTable<T>();

            SQLiteCommand cmd = new(DB);
            cmd.CommandText = $"SELECT * FROM {typeof(T).Name};";
            cmd.Prepare();
            SQLiteDataReader sqLiteDataReader = cmd.ExecuteReader();

            while (sqLiteDataReader.Read()) {
                dynamic byId = GetReader<T>().Invoke(sqLiteDataReader);
                yield return byId;
            }

            throw new Exception();

        }

        public static bool Insert<T>(T obj) where T : ISQLEntity {
            check();
            CheckTable<T>();

            SQLiteCommand cmd = new(DB);
            string[] a = obj.InsertKeys();
            string keys = string.Join(",", a);
            string vals = string.Join(",", a.Select(x => $"@{x}"));

            cmd.CommandText = $"INSERT INTO {typeof(T).Name}({keys}) VALUES({vals});";
            obj.InsertArgs(cmd);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool Update<T>(T obj) where T : ISQLEntity {
            check();
            CheckTable<T>();

            string[] a = obj.UpdateKeys();

            SQLiteCommand cmd = new(DB);
            string[] setterArr = a.Select(s => $"{s} = @{s}").ToArray();
            string setters = string.Join(", ", setterArr);

            cmd.CommandText = $"UPDATE {typeof(T).Name} SET {setters} WHERE id=@id;";
            cmd.Parameters.AddWithValue("@id", obj.GetId());
            obj.UpdateArgs(cmd);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }

        public static void TryInsertOrUpdate<T>(T obj) where T : ISQLEntity {
            if (obj.IsStored())
                Update(obj);
            else
                Insert(obj);
        }


        public static class Utils {
            public static Expression<Func<T, bool>> ConvertPredicate<T>(Predicate<T> predicate) {
                ParameterExpression p0 = Expression.Parameter(typeof(T));
                return Expression.Lambda<Func<T, bool>>(Expression.Call(predicate.Method, p0), p0);
            }
        }

    }
}