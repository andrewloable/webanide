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
        public int ID { get; set; }
        public string Target { get; set; }
        public string Session { get; set; }
        public int Window { get; set; }
        public Dictionary<int, Result> Pending { get; set; }
        public Dictionary<string, BindingFunction> Bindings { get; set; }
        public ClientWebSocket WSClient { get; set; }
        public Process ChromeCommand { get; set; }

        public static async Task<Chrome> ChromeFactory(string binaryPath, string args)
        {
            var retval = new Chrome()
            {
                ID = 2,
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
        public static string LocateChrome(string customPath = "")
        {
            var envpath = Environment.GetEnvironmentVariable("ChromePath");
            if (!string.IsNullOrWhiteSpace(envpath) && File.Exists(envpath))
            {
                return envpath;
            }

            if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            {
                return customPath;
            }

            string[] Paths;
            if (OperatingSystem.IsMacOS())
            {
                Paths = new string[]
                {
                    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                    "/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary",
                    "/Applications/Chromium.app/Contents/MacOS/Chromium",
                    "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
                    "/usr/bin/google-chrome-stable",
                    "/usr/bin/google-chrome",
                    "/usr/bin/chromium",
                    "/usr/bin/chromium-browser"
                };
            }
            else if (OperatingSystem.IsWindows())
            {
                const string chromepath = "/Google/Chrome/Application/chrome.exe";
                const string edgepath = "/Microsoft/Edge/Application/msedge.exe";
                const string chromiumpath = "/Chromium/Application/chrome.exe";
                Paths = new string[]
                {
                    $"{Environment.GetEnvironmentVariable("LocalAppData")}{chromepath}",
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}{chromepath}",
                    $"{Environment.GetEnvironmentVariable("ProgramFiles(x86)")}{chromepath}",
                    $"{Environment.GetEnvironmentVariable("LocalAppData")}{chromiumpath}",
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}{chromiumpath}",
                    $"{Environment.GetEnvironmentVariable("ProgramFiles(x86)")}{chromiumpath}",
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}{edgepath}",
                    $"{Environment.GetEnvironmentVariable("ProgramFiles(x86)")}{edgepath}",
                };
            }
            else
            {
                Paths = new string[]
                {
                    "/usr/bin/google-chrome-stable",
                    "/usr/bin/google-chrome",
                    "/usr/bin/chromium",
                    "/usr/bin/chromium-browser",
                    "/snap/bin/chromium"
                };
            }
            foreach(var p in Paths)
            {
                if (!File.Exists(p))
                {
                    continue;
                }
                return p;
            }

            throw new Exception("Chrome/Chromium/MS Edge Path not found");
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
                    var test1 = ws.State;
                    var test2 = ws.CloseStatus;
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
            this.ID++;
            var navigatemsg = new Message()
            {
                ID = this.ID,
                Method = "Page.navigate",
                Params = p
            };
            var pm = new JObject();
            p["message"] = JObject.FromObject(navigatemsg);
            p["sessionId"] = this.Session;
            var msg = new Message()
            {
                ID = this.ID,
                Method = "Target.sendMessageToTarget",
                Params = pm
            };
            await this.WSClient.SendAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg)), WebSocketMessageType.Text, true, CancellationToken.None);

        }
    }
}
