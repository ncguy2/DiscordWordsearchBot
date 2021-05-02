using Newtonsoft.Json;

namespace WordSearchBot.Core.Utils {
    public interface IJsonable {
        
    }

    public static class JsonableExtension {
        public static string ToJson(this IJsonable obj) {
            return JsonConvert.SerializeObject(obj);
        }
    }

}