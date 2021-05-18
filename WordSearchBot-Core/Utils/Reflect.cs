using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WordSearchBot.Core.Utils {
    public class Reflect<T> {
        private T obj;
        private Type Type => obj.GetType();

        private FieldInfo[] _fields;

        private FieldInfo[] Fields {
            get { return _fields ??= Type.GetFields(); }
        }

        public Reflect(T obj) {
            this.obj = obj;
        }

        public FieldInfo GetFieldWithAttribute<U>() where U: Attribute {
            return Fields.First(x => x.GetCustomAttribute<U>() != null);
        }

        public FieldInfo[] GetFieldsWithAttribute<U>() where U : Attribute {
            return Fields
                       .Where(x => x.GetCustomAttribute<U>() != null)
                       .ToArray();
        }

        public IEnumerable<FieldInfo> GetFields(Predicate<FieldInfo> predicate) {
            return Fields.Where(x => predicate(x));
        }

    }
}