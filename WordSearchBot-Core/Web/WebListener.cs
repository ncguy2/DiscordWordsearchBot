using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Discord.WebSocket;
using Newtonsoft.Json;
using WordSearchBot.Core.Web.REST;

namespace WordSearchBot.Core.Web {
    public class WebListener {
        protected Core discord;
        protected HttpListener httpListener;
        protected Thread thread;
        protected Endpoints endpoints;
        public bool Active;

        public WebListener(Core discord, Endpoints endpoints) {
            this.discord = discord;
            this.endpoints = endpoints;
        }

        public void Start() {
            httpListener = new HttpListener();
            // httpListener.Prefixes.Add("http://nick-aws.ddns.net/");
            httpListener.Prefixes.Add("http://127.0.0.1:8080/");

            Active = true;
            thread = new Thread(Loop) {
                IsBackground = false
            };
            thread.Start();
        }

        protected void Loop() {
            httpListener.Start();

            while (Active) {
                HttpListenerContext ctx = httpListener.GetContext();
                HttpListenerRequest request = ctx.Request;
                HttpListenerResponse response = ctx.Response;

                Console.WriteLine("");

                RequestContext context = new(ctx);
                context.discord = this.discord;

                // string args = strings.Length > 1 ? strings[1] : "";
                RESTEndpoint endpoint = endpoints.Get(context.path);
                try {
                    if (endpoint is IDataEndpoint d) {
                        DataPayload payload = d.DoWork(context);

                        foreach (KeyValuePair<HttpResponseHeader, string> p in payload.StandardHeaders)
                            response.Headers.Set(p.Key, p.Value);
                        foreach (KeyValuePair<string, string> p in payload.CustomHeaders)
                            response.Headers.Add(p.Key, p.Value);

                        response.OutputStream.Write(payload.Data, 0, payload.Data.Length);
                        response.OutputStream.Close();
                    }
                } catch (Exception e) {
                    response.StatusCode = 500;
                    byte[] b = Encoding.UTF8.GetBytes(e.Message);
                    response.OutputStream.Write(b, 0, b.Length);
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.ToString());
                    response.OutputStream.Close();
                }
            }
        }
    }

    public struct RequestContext {
        public Core discord;
        public HttpListenerContext httpContext;
        public string path;
        public Dictionary<string, string> arguments;

        public RequestContext(HttpListenerContext httpContext) : this() {
            this.httpContext = httpContext;
            arguments = new Dictionary<string, string>();

            string[] strings = httpContext.Request.RawUrl.Split("?");
            path = strings[0];

            if (strings.Length <= 1)
                return;

            string args = strings[1];

            string[] argPairs = args.Split("&");
            foreach (string argPair in argPairs) {
                string[] a = argPair.Split("=");
                arguments.Add(a[0], a[1]);
            }
        }
    }

}