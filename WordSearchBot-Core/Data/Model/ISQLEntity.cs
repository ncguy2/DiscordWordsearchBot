using System.Data.SQLite;

namespace WordSearchBot.Core.Model {
    public interface ISQLEntity {
        string[] InsertKeys();
        void InsertArgs(SQLiteCommand cmd);

        string[] UpdateKeys();
        void UpdateArgs(SQLiteCommand cmd);

        long GetId();
    }

    public static class ISQLEntityExtension {
        public static bool IsStored(this ISQLEntity entity) {
            return entity.GetId() >= 0;
        }
    }
}