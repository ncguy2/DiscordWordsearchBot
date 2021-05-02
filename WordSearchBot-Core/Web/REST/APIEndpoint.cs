using WordSearchBot.Core.Model;
using WordSearchBot.Core.Web.REST.api;

namespace WordSearchBot.Core.Web.REST {
    public class APIEndpoint : RESTEndpoint {
        public override string GetPath() {
            return "api";
        }

        public override void SubEndpoints(Endpoints endpoints) {
            endpoints.Register<SuggestionsEndpoint>();
        }
    }
}