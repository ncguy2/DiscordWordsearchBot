using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WordSearchBot.Core {
    public class PersistentList<T> : PersistentData<IEnumerable<T>, T> {
        protected List<T> data;

        protected List<T> Data => data ??= new List<T>();

        public PersistentList(string backingFile, DataSerialiser<T> Serialiser) : base(backingFile) {
            this.Serialiser = Serialiser;
        }

        protected override IEnumerable<T> GetImpl() {
            return Data;
        }

        public void Add(T item,  bool write = true) {
            Data.Add(item);
            if(write)
                Write();
        }
        public void AddRange(IEnumerable<T> items, bool write = true) {
            Data.AddRange(items);
            if(write)
                Write();
        }

        protected override void SetImpl(IEnumerable<T> t) {
            Data.Clear();
            Data.AddRange(t);
        }

        protected override void Read() {
            SetImpl(File.ReadLines(BackingFile).Select(Serialiser.Deserialize));
            IsFileDirty = false;
        }

        protected override void Write() {
            List<string> lines = GetImpl().Select(Serialiser.Serialize).ToList();
            File.WriteAllLines(BackingFile, lines);
        }

        public List<T> AsList() {
            return Data;
        }
    }
}