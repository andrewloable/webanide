using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Webanide.Models
{
    public class Config
    {
        [JsonProperty("browser-path", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BrowserPath BrowserPath { get; set; }
        [JsonProperty("default-args", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] DefaultArgs { get; set; }
    }

    public class BrowserPath
    {
        [JsonProperty("macos", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] MacOS { get; set; }
        [JsonProperty("linux", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] Linux { get; set; }
        [JsonProperty("windows", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public WindowsBrowserPath Windows { get; set; }
    }
    public class WindowsBrowserPath
    {
        [JsonProperty("chromepath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ChromePath { get; set; }
        [JsonProperty("chromiumpath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ChromiumPath { get; set; }
        [JsonProperty("edgepath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EdgePath { get; set; }
    }
}
