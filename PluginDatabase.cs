using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DalamudPluginServer
{
	public class PluginDatabase
	{
		private string pluginRepo;

		private string pluginsPath;
		private string pluginRoot { get { return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), pluginsPath); } }

		private Dictionary<string, Plugin> pluginJson;
		private Dictionary<string, PluginPaths> pluginPaths;


		public PluginDatabase(string _pluginsPath)
		{
			pluginsPath = _pluginsPath;
			if (!Directory.Exists(pluginRoot)) Directory.CreateDirectory(pluginRoot);
			GeneratePluginRepo();
		}

		internal string GetPluginRepo()
		{
			if (pluginRepo == null)
			{
				GeneratePluginRepo();
			}
			return pluginRepo;
		}

		private void GeneratePluginRepo()
		{
			LoadPluginsFromDisk();

			Plugin[] plugins = pluginJson.Values.ToArray();
			pluginRepo = JsonConvert.SerializeObject(plugins);
		}

		private void LoadPluginsFromDisk()
		{
			pluginJson = new Dictionary<string, Plugin>();
			pluginPaths = new Dictionary<string, PluginPaths>();

			List<string> pD = GetPluginDirectories();

			// need to add the following parameters to all the json
			// RepoUrl CategoryTags IconUrl ImageUrls DownloadLinkInstall IsHide
			// IsTestingExclusive DownloadLinkTesting DownloadLinkUpdate
			// DownloadCount LastUpdate

			foreach (string p in pD) 
			{
				string jsonPath = null;
				string iconPath = null;
				string zipPath = null;

				List<string> files = Directory.GetFiles(p).ToList();

				foreach (string f in files)
				{
					string ext = Path.GetExtension(f);
					switch (ext)
					{
						case ".zip":
							zipPath = f;
							break;
						case ".json":
							jsonPath = f;
							break;
						case ".png":
							iconPath = f;
							break;
						default:
							continue;
					}
				}

				if (jsonPath == null) 
				{
					Console.WriteLine($"Missing json in plugin directory: {p}");
					continue;
				};
				if (zipPath == null)
				{
					Console.WriteLine($"Missing zip in plugin directory: {p}");
					continue;
				};
				if (iconPath == null)
				{
					// missing icon is okay, just a warning
					Console.WriteLine($"Missing icon in plugin directory: {p}");
				};

				string jsonString = File.ReadAllText(jsonPath); 
				Plugin plugin = JsonConvert.DeserializeObject<Plugin>(jsonString);

				plugin.DownloadCount = 0;
				plugin.LastUpdate = EpochTime.GetIntDate(DateTime.Now);
				plugin.DownloadLinkInstall = WebServer.CreatePluginDownloadUrl(plugin);
				plugin.DownloadLinkUpdate = WebServer.CreatePluginDownloadUrl(plugin);
				plugin.IconUrl = WebServer.CreatePluginIconUrl(plugin);

				PluginPaths _pluginPaths = new PluginPaths();
				_pluginPaths.IconPath = iconPath;
				_pluginPaths.ZipPath = zipPath;

				if (plugin.InternalName == string.Empty || plugin.InternalName == null)
				{
					Console.WriteLine($"Invalid InternalName in plugin json: {p}");
					continue;
				}

				pluginJson[plugin.InternalName] = plugin;
				pluginPaths[plugin.InternalName] = _pluginPaths;
			}
		}

		private List<string> GetPluginDirectories()
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			return Directory.EnumerateDirectories(pluginRoot).ToList();
		}

		internal byte[] GetZip(string pluginInternalName)
		{
			PluginPaths plugin = pluginPaths[pluginInternalName];
			byte[] bytes = File.ReadAllBytes(plugin.ZipPath);
			return bytes;
		}

		internal byte[] GetPluginIcon(string pluginInternalName)
		{
			PluginPaths plugin = pluginPaths[pluginInternalName];
			byte[] bytes = File.ReadAllBytes(plugin.IconPath);
			return bytes;
		}

		public struct PluginPaths
		{
			public string PluginName { get; set; }
			public string IconPath { get; set; }
			public string ZipPath { get; set; }

		}

		public class Plugin
		{
			// Basic Information
			public string Author { get; set; } //
			public string Name { get; set; } //
			public string Punchline { get; set; } //
			public string Description { get; set; } //

			// Metadata
			public List<string> Tags { get; set; } = new List<string>(); //
			public string InternalName { get; set; } //
			public string RepoUrl { get; set; } // Need to add in API
			public long DownloadCount { get; set; } // Need to add in API
			public long LastUpdate { get; set; } // Need to add in API

			// Download Links
			public string DownloadLinkInstall { get; set; } // Need to add in API
			public string DownloadLinkUpdate { get; set; } // Need to add in API

			// Versioning
			public string AssemblyVersion { get; set; } //
			public string ApplicableVersion { get; set; } // 
			public string DalamudApiLevel { get; set; } // 

			// testing, not needed
			public string TestingAssemblyVersion { get; set; } // Needed by API
			public string TestingDalamudApiLevel { get; set; } // Needed by API
			public string DownloadLinkTesting { get; set; } // Needed by API

			// UI
			public string IconUrl { get; set; } // Need to add in API

			// Additional Information
			public string Changelog { get; set; } // Need to add in API
		}
	}
}