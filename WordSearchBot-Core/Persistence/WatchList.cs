namespace WordSearchBot.Core {
    public class WatchList<T, SER> : PersistentList<T> where SER : DataSerialiser<T>, new() {

        public WatchList(string backingFile) : base(backingFile, new SER()) { }

    }
}