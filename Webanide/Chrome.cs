using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Webanide.Models;

namespace Webanide
{
    public class Chrome
    {
        public string ID { get; set; }
        public int MessageID { get; set; }
        public string Target { get; set; }
        public string Session { get; set; }
        public int Window { get; set; }
        public Dictionary<int, Result> Pending { get; set; }
        public Dictionary<string, BindingFunction> Bindings { get; set; }
        public ClientWebSocket WSClient { get; set; }
        public Process ChromeCommand { get; set; }
        public Config Config { get; set; }
        public Chrome()
        {
            Config = GetConfig();
        }
        public static async Task<Chrome> ChromeFactory(string binaryPath, string args)
        {
            var retval = new Chrome()
            {
                MessageID = 2,
                Pending = new Dictionary<int, Result>(),
                Bindings = new Dictionary<string, BindingFunction>()
            };
            // start chrome process
            var procinfo = new ProcessStartInfo(binaryPath, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            retval.ChromeCommand = Process.Start(procinfo);
            var standardError = retval.ChromeCommand.StandardError;
            string errOutput;
            do
            {
                errOutput = standardError.ReadLine();
                if (!string.IsNullOrEmpty(errOutput) && errOutput.Contains("DevTools"))
                {
                    break;
                }
            }
            while (true);

            // wait for websocket address from stderr
            var re = new Regex(@"^DevTools listening on (ws:\/\/.*?)$");
            var matches = re.Match(errOutput);
            var wsURL = matches.Groups[1].Value;
            // open websocket
            retval.WSClient = new ClientWebSocket();
            await retval.WSClient.ConnectAsync(new Uri(wsURL), CancellationToken.None);
            // find target
            retval.Target = await retval.FindTarget();
            retval.Session = await retval.StartSession(retval.Target);
            return retval;
        }
        private static Config GetConfig()
        {
            if (File.Exists("webanide-config.json"))
            {
                var configText = File.ReadAllText("webanide-config.json");
                var config = JObject.Parse(configText).ToObject<Config>();
                return config;
            }
            throw new Exception("webanide-config.json not found.");
        }
        
        private static async Task<string[]> GetWSResponse(ClientWebSocket ws, int msgid)
        {
            var retval = new List<string>();
            WebSocketReceiveResult result;
            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);


                    ms.Seek(0, SeekOrigin.Begin);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            var txt = await reader.ReadToEndAsync();
                            var j = JObject.Parse(txt);
                            if (j["id"] != null && j["id"].ToString() == msgid.ToString())
                            {
                                retval.Add(txt);
                                return retval.ToArray();
                            }
                            retval.Add(txt);
                        }
                    }
                }
            } while (true);
        }
        public async Task<string> FindTarget()
        {
            var p = new JObject();
            p["discover"] = true;
            var msg = new Message()
            {
                ID = 0,
                Method = "Target.setDiscoverTargets",
                Params = p
            };
            var payload = JsonConvert.SerializeObject(msg);
            await this.WSClient.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, CancellationToken.None);
            var res = await GetWSResponse(this.WSClient, msg.ID);
            foreach(var o in res)
            {
                var resp = o.Substring(0, o.Length);
                var respmessage = JsonConvert.DeserializeObject<Message>(resp);
                if (respmessage.Method == "Target.targetCreated")
                {
                    var target = respmessage.Params;
                    if (target["targetInfo"]["type"].ToString() == "page")
                    {
                        return target["targetInfo"]["targetId"].ToString();
                    }
                }
            }
            throw new Exception("No Target ID Found");
        }
        public async Task<string> StartSession(string target)
        {
            var p = new JObject();
            p["targetId"] = target;
            var msg = new Message()
            {
                ID = 1,
                Method = "Target.attachToTarget",
                Params = p
            };
            var payload = JsonConvert.SerializeObject(msg);
            await this.WSClient.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, CancellationToken.None);
            var res = await GetWSResponse(this.WSClient, msg.ID);
            foreach (var o in res)
            {
                var resp = o.Substring(0, o.Length);
                var respmessage = JObject.Parse(resp);
                if ((respmessage["id"]?.ToString() ?? "") == 1.ToString())
                {
                    var session = respmessage["result"];
                    return session["sessionId"].ToString();
                }
            }
            throw new Exception("No Target ID Found");
        }
        public async void LoadUrl(string url)
        {
            var p = new JObject();
            p["url"] = url;
            this.MessageID++;
            var navigatemsg = new Message()
            {
                ID = this.MessageID,
                Method = "Page.navigate",
                Params = p
            };
            var pm = new JObject();
            p["message"] = JObject.FromObject(navigatemsg);
            p["sessionId"] = this.Session;
            var msg = new Message()
            {
                ID = this.MessageID,
                Method = "Target.sendMessageToTarget",
                Params = pm
            };
            await this.WSClient.SendAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

        }
    }
}
