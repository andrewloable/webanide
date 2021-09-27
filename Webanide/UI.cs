using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Webanide.Models;

namespace Webanide
{
	public class UI
	{
        #region Structs
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        #endregion
        #region Windows P/Invoke
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        #endregion

        #region Constants
        const int SW_MAXIMIZE = 3;
        const int SW_MINIMIZE = 6;
        const int SW_NORMAL = 1;
        const int GWL_STYLE = -16;
        const int WS_BORDER = 0x00800000;
        const int WS_CAPTION = 0x00C00000;
        const int WS_MAXIMIZEBOX = 0x00010000;
        const int WS_MINIMIZEBOX = 0x00020000;
        const int WS_SIZEBOX = 0x00040000;
        const int WS_THICKFRAME = 0x00040000;
        const int NOFRAME = WS_BORDER | WS_CAPTION | WS_MAXIMIZEBOX | WS_MINIMIZEBOX | WS_SIZEBOX | WS_THICKFRAME;
        #endregion

        public Chrome Chrome { get; set; }
		public string TemporaryDirectory { get; set; }
        private static Config Config { get; set; }
		public static async Task<UI> New(string url, string dir = "", int width = 1920, int height = 1080, params string[] customerArgs)
        {
			var uid = Guid.NewGuid().ToString();
			var retval = new UI();
            var initurl = $@"data:text/html,<html><head><title>{uid}</title></html>";
            retval.TemporaryDirectory = dir;
			if (string.IsNullOrWhiteSpace(dir))
            {
				var name = Path.Combine(Path.GetTempPath(), uid);
				retval.TemporaryDirectory = name;
            }
            Config = GetConfig();
			var args = new List<string>();
			args.Add($"--app={initurl}");
			args.Add($"--user-data-dir={retval.TemporaryDirectory}");
			args.AddRange(Config.DefaultArgs);
			args.Add("--remote-debugging-port=0");

			retval.Chrome = await Chrome.ChromeFactory(LocateChrome(), string.Join(" ", args.ToArray()));
            retval.Chrome.ID = uid;
            SetUIPosition(retval.Chrome, 0, 0);
            SetUISize(retval.Chrome, width, height);
            SetWindowState(retval.Chrome, WindowState.Normal);

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
        private static string LocateChrome(string customPath = "")
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
                Paths = Config.BrowserPath.MacOS;
            }
            else if (OperatingSystem.IsWindows())
            {
                var chromepath = Config.BrowserPath.Windows.ChromePath;
                var edgepath = Config.BrowserPath.Windows.EdgePath;
                var chromiumpath = Config.BrowserPath.Windows.ChromiumPath;
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
                Paths = Config.BrowserPath.Linux;
            }
            foreach (var p in Paths)
            {
                if (!File.Exists(p))
                {
                    continue;
                }
                return p;
            }

            throw new Exception("Chrome/Chromium/MS Edge Path not found");
        }
        private static void SetUIPosition(Chrome chrome, int left, int top)
        {
            if (OperatingSystem.IsWindows())
            {
                var window = Process.GetProcesses().FirstOrDefault(r => r.MainWindowTitle == chrome.ID);
                var hWnd = window.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    var rect = new RECT();
                    if (GetWindowRect(hWnd, ref rect))
                    {
                        MoveWindow(hWnd, left, top, rect.right-rect.left, rect.bottom-rect.top, true);
                    }
                }
                else
                {
                    throw new Exception("Chrome Window Not Found");
                }
            }
            else
            {
                throw new NotImplementedException("Linux and MacOS SetUIBounds not yet implemented");
            }
        }
        private static void SetUISize(Chrome chrome, int width, int height)
        {
            if (OperatingSystem.IsWindows())
            {
                var window = Process.GetProcesses().FirstOrDefault(r => r.MainWindowTitle == chrome.ID);
                var hWnd = window.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    var rect = new RECT();
                    if (GetWindowRect(hWnd, ref rect))
                    {
                        MoveWindow(hWnd, rect.left, rect.top, width, height, true);
                    }
                } 
                else
                {
                    throw new Exception("Chrome Window Not Found");
                }
            }
            else
            {
                throw new NotImplementedException("Linux and MacOS SetUIBounds not yet implemented");
            }
        }
        private static void SetWindowState(Chrome chrome, WindowState state)
        {
            if (OperatingSystem.IsWindows())
            {
                var window = Process.GetProcesses().FirstOrDefault(r => r.MainWindowTitle == chrome.ID);
                var hWnd = window.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    switch(state)
                    {
                        case WindowState.Maximized:
                            ShowWindow(hWnd, SW_MAXIMIZE);
                            break;
                        case WindowState.Minimized:
                            ShowWindow(hWnd, SW_MINIMIZE);
                            break;
                        case WindowState.NoFrame:
                            var style = GetWindowLong(hWnd, GWL_STYLE);
                            _ = SetWindowLong(hWnd, GWL_STYLE, (style & ~NOFRAME));
                            break;
                        case WindowState.Normal:
                            ShowWindow(hWnd, SW_NORMAL);
                            break;
                    }
                }
                else
                {
                    throw new Exception("Chrome Window Not Found");
                }
            }
            else
            {
                throw new NotImplementedException("Linux and MacOS SetUIBounds not yet implemented");
            }
        }
    }
}
