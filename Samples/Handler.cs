using Newtonsoft.Json.Linq;

namespace Samples
{
    public class Handler
    {
        public string Get(string name) => $"Hello {name}";

        public JObject Post(JObject obj)
        {
            return obj;
        }
    }
}
