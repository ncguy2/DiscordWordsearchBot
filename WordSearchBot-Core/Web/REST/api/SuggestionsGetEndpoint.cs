using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using WordSearchBot.Core.Data.Facade;
using WordSearchBot.Core.Model;

namespace WordSearchBot.Core.Web.REST.api {
    public class SuggestionsGetEndpoint : RESTEndpoint, IDataEndpoint {

        private Suggestions suggestions;

        public SuggestionsGetEndpoint() {
            suggestions = new Suggestions();
        }

        public override string GetPath() {
            return "get";
        }

        public DataPayload DoWork(RequestContext context) {
            return new (suggestions.Get(context.arguments["key"]));
        }

    }
}