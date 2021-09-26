using Newtonsoft.Json;

namespace Webanide.Models
{
    public class WindowTargetMessage
    {
        [JsonProperty("windowId")]
        public int WindowID { get; set; }
        [JsonProperty("bounds")]
        public Bounds Bounds { get; set; }
    }
}
