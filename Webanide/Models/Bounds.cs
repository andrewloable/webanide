using Newtonsoft.Json;

namespace Webanide.Models
{
    public class Bounds
    {
        [JsonProperty("left")]
        public int Left { get; set; }
        [JsonProperty("top")]
        public int Top { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
        [JsonProperty("windowState")]
        public WindowState WindowState { get; set; }
    }
}
