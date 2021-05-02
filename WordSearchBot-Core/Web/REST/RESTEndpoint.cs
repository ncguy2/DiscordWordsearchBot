namespace WordSearchBot.Core.Web.REST {
    public abstract class RESTEndpoint {

        public abstract string GetPath();

        public virtual void SubEndpoints(Endpoints endpoints) {}

    }

}