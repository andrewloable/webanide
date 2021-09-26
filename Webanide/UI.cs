using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Webanide
{
	public class UI
	{
		public Chrome Chrome { get; set; }
		public string TemporaryDirectory { get; set; }
		public List<string> DefaultChromeArgs = new List<string> {
			"--disable-background-networking",
			"--disable-background-timer-throttling",
			"--disable-backgrounding-occluded-windows",
			"--disable-breakpad",
			"--disable-client-side-phishing-detection",
			"--disable-default-apps",
			"--disable-dev-shm-usage",
			"--disable-infobars",
			"--disable-extensions",
			"--disable-features=site-per-process",
			"--disable-hang-monitor",
			"--disable-ipc-flooding-protection",
			"--disable-popup-blocking",
			"--disable-prompt-on-repost",
			"--disable-renderer-backgrounding",
			"--disable-sync",
			"--disable-translate",
			"--disable-windows10-custom-titlebar",
			"--metrics-recording-only",
			"--no-first-run",
			"--no-default-browser-check",
			"--safebrowsing-disable-auto-update",
			"--enable-automation",
			"--password-store=basic",
			"--use-mock-keychain"
		};
		public static async Task<UI> New(string url, string dir, int width, int height, params string[] customerArgs)
        {
			var retval = new UI();
			if (string.IsNullOrWhiteSpace(url))
            {
				url = "data:text/html,<html></html>";
            }
			retval.TemporaryDirectory = dir;
			if (string.IsNullOrWhiteSpace(dir))
            {
				var name = Path.GetTempPath();
				retval.TemporaryDirectory = name;
            }
			var args = new List<string>();
			args.Add($"--app={url}");
			args.Add($"--user-data-dir={retval.TemporaryDirectory}");
			args.Add($"--window-size={width},{height}");
			args.AddRange(retval.DefaultChromeArgs.ToArray());
			args.Add("--remote-debugging-port=0");

			retval.Chrome = await Chrome.ChromeFactory(Chrome.LocateChrome(), string.Join(" ", args.ToArray()));

			return retval;
        }
    }
}
