using WordSearchBot.Core.Web.REST;

namespace WordSearchBot.Core.Web {
    public class WebInterface {

        protected Endpoints endpoints;
        protected WebListener listener;

        public WebInterface(Core core, int port = -1) {
            endpoints = new Endpoints();
            listener = new WebListener(core, endpoints, port);
        }

        public void Initialise() {
            endpoints.Register<APIEndpoint>();

            listener.Start();
        }

    }
}