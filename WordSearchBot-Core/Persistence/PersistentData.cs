using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WordSearchBot.Core {
    public abstract class PersistentData<T, T_SER> {

        protected string BackingFile;
        protected bool IsFileDirty = true;
        protected DataSerialiser<T_SER> Serialiser;

        protected PersistentData(string backingFile) {
            BackingFile = backingFile;
        }

        public T Get() {
            if(IsFileDirty) {
                Read();
            }
            return GetImpl();
        }

        public void Set(T t) {
            SetImpl(t);
            Write();
        }

        protected abstract T GetImpl();
        protected abstract void SetImpl(T t);


        protected abstract void Read();
        protected abstract void Write();
    }

    public abstract class DataSerialiser<T> {

        public abstract T Deserialize(string str);
        public abstract string Serialize(T obj);
    }


}