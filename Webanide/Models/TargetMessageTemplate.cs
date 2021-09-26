using Newtonsoft.Json;

namespace Webanide.Models
{
    public class TargetMessageTemplate
    {
        [JsonProperty("id")]
        public int ID { get; set; }
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("params")]
        public TargetMessageTemplateParams Params { get; set; }
        [JsonProperty("error")]
        public TargetMessageTemplateError Error { get; set; }
        [JsonProperty("result")]
        public string Result { get; set; }
    }
    public class TargetMessageTemplateParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("payload")]
        public string Payload { get; set; }
        [JsonProperty("executionContextId")]
        public int ID { get; set; }
        [JsonProperty("args")]
        public TargetMessageTemplateParamsArgs[] Args { get; set; }
    }
    public class TargetMessageTemplateParamsArgs
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("value")]
        public dynamic Value { get; set; }
    }
    public class TargetMessageTemplateError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
