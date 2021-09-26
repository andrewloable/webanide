using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Webanide.Models
{
    public class Message
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int ID { get; set; }
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public string Result { get; set; }
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public string Error { get; set; }
        [JsonProperty("method", NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }
        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Params { get; set; }
    }
}
