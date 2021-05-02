using System;
using LiteDB;
using Newtonsoft.Json;
using JsonReader = Newtonsoft.Json.JsonReader;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using JsonWriter = Newtonsoft.Json.JsonWriter;

namespace WordSearchBot.Core.Utils.Json {
    public class ObjectIdSerializer : JsonConverter<ObjectId> {

        public override void WriteJson(JsonWriter writer, ObjectId? value, JsonSerializer serializer) {
            if(value == null)
                writer.WriteNull();
            else
                writer.WriteValue(value.ToString());
        }

        public override ObjectId? ReadJson(JsonReader reader, Type objectType, ObjectId? existingValue, bool hasExistingValue,
                                           JsonSerializer serializer) {
            string? str = reader.ReadAsString();

            return str != null ? new ObjectId(str) : null;
        }
    }
}