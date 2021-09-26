using Newtonsoft.Json;

namespace Webanide.Models
{
    public class TargetMessage
    {
        public TargetMessageTemplate TargetMessageTemplate { get; set; }
        [JsonProperty("result")]
        public TargetMessageResult Result { get; set; }
    }
    public class TargetMessageResult
    {
        [JsonProperty("result")]
        public TargetMessageResultResult Result { get; set; }
        [JsonProperty("exceptionDetails")]
        public TargetMessageExceptionDetails Exception { get; set; }
    }
    public class TargetMessageResultResult
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
    }
    public class TargetMessageExceptionDetails
    {
        [JsonProperty("exception")]
        public TargetMessageExeption Exception { get; set; }
    }
    public class TargetMessageExeption
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
