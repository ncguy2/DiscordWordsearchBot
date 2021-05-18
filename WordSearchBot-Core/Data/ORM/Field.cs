using System;
using System.Reflection;

namespace WordSearchBot.Core.Data.ORM {

    [AttributeUsage(AttributeTargets.Field)]
    public class FieldAttribute : Attribute {

        private readonly string FieldName;

        public FieldAttribute(string fieldName) {
            FieldName = fieldName;
        }

        public string GetName(FieldInfo field) {
            return FieldName ?? field.Name;
        }

    }
}