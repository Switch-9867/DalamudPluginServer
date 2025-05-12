using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DalamudPluginServer
{
	public partial class PluginDatabase
	{
		public static string PLUGIN_REPO { get; private set; }
		public static List<Plugin> Plugins { get; private set; }

		private Dictionary<string, PluginFileInfo> PluginsFiles;

		public PluginDatabase()
		{
			PluginsFiles = new Dictionary<string, PluginFileInfo>();
		}

		public void UpdateRepo()
		{
			Plugins = CreatePluginDictionary();
			PLUGIN_REPO = GenerateRepoString();
		}

		internal string GetPluginRepo()
		{
			if (PLUGIN_REPO == null) GenerateRepoString();
			return PLUGIN_REPO;
		}

		private string GenerateRepoString()
		{
			Plugin[] plugins = Plugins.ToArray();
			return JsonConvert.SerializeObject(plugins);
		}

		private List<Plugin> CreatePluginDictionary()
		{
			List<Plugin> plugins = new List<Plugin>();

			foreach (PluginFileInfo pluginInfo in PluginsFiles.Values) 
			{
				Plugin p = Plugin.FromFileInfo(pluginInfo);
				plugins.Add(p);
			}

			return plugins;
		}

		internal byte[] GetZipBytes(string pluginInternalName)
		{
			PluginFileInfo plugin = PluginsFiles[pluginInternalName];
			byte[] bytes = File.ReadAllBytes(plugin.PluginZip);
			return bytes;
		}

		internal byte[] GetPluginIconBytes(string pluginInternalName)
		{
			PluginFileInfo plugin = PluginsFiles[pluginInternalName];
			byte[] bytes = File.ReadAllBytes(plugin.PluginIcon);
			return bytes;
		}

		internal void AddPlugins(List<PluginFileInfo> pluginFileInfos)
		{
			foreach (PluginFileInfo pluginFileInfo in pluginFileInfos) 
			{
				AddPlugin(pluginFileInfo); 
			}
		}
		internal void AddPlugin(PluginFileInfo pluginFileInfo)
		{
			PluginsFiles.Add(pluginFileInfo.PluginName, pluginFileInfo);
		}
	}
}