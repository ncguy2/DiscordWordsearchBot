using System.Collections.Generic;
using System.Net;
using System.Text;
using WordSearchBot.Core.Utils;

namespace WordSearchBot.Core.Web.REST {
    public interface IDataEndpoint {
        DataPayload DoWork(RequestContext context);
    }

    public class DataPayload {
        public Dictionary<HttpResponseHeader, string> StandardHeaders;
        public Dictionary<string, string> CustomHeaders;
        public byte[] Data;

        public DataPayload() {
            StandardHeaders = new Dictionary<HttpResponseHeader, string>();
            CustomHeaders = new Dictionary<string, string>();
        }

        public DataPayload(byte[] data) : this() {
            Data = data;
        }

        public DataPayload(string data) : this(Encoding.UTF8.GetBytes(data)) {}

        public DataPayload(IJsonable data) : this(data.ToJson()) {
            StandardHeaders[HttpResponseHeader.ContentType] = "application/json";
        }
    }

}