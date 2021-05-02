using System;
using System.Collections.Generic;
using System.Linq;
using WordSearchBot.Core.Web.REST;

namespace WordSearchBot.Core.Web {
    public class Endpoints {

        protected Dictionary<string, RESTEndpoint> endpoints;

        protected Stack<RESTEndpoint> endpointStack;

        public Endpoints() {
            endpoints = new Dictionary<string, RESTEndpoint>();
            endpointStack = new Stack<RESTEndpoint>();
        }

        public void Register<T>() where T : RESTEndpoint, new() {
            Register(new T());
        }

        public RESTEndpoint Get(string path) {
            path = path.ToLower();
            return endpoints.ContainsKey(path) ? endpoints[path] : null;
        }

        public void Push(RESTEndpoint endpoint) {
            endpointStack.Push(endpoint);
        }

        public void Pop() {
            endpointStack.Pop();
        }

        protected string GetPath() {
            return endpointStack.Reverse().Aggregate("/", (current, e) => $"{current}{e.GetPath()}/");
        }

        protected void Register(RESTEndpoint endpoint) {
            string s = (GetPath() + endpoint.GetPath()).ToLower();
            if (endpoints.ContainsKey(s))
                throw new Exception($"Duplicate path @ \"{s}\"");
            endpoints.Add(s, endpoint);

            RegisterSubEndpoints(endpoint);
        }

        protected void RegisterSubEndpoints(RESTEndpoint endpoint) {
            Push(endpoint);
            endpoint.SubEndpoints(this);
            Pop();
        }
    }
}