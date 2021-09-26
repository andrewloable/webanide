using Newtonsoft.Json;

namespace Webanide.Models
{
    public class Result
    {   
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("subtype")]
        public string Subtype { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("objectId")]
        public string ObjectID { get; set; }
        public string Error { get; set; }
    }
}
