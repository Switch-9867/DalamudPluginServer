using System;
using System.IO;

namespace DalamudPluginServer
{
	internal struct PluginFileInfo
	{
		public string PluginFolder { get; }

		public PluginFileInfo(string pluginFolder)
		{
			PluginFolder = pluginFolder;
		}

		public string PluginName => Path.GetFileName(PluginFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

		public string PluginZip => Path.Combine(PluginFolder, "latest.zip");

		public string PluginJson => Path.Combine(PluginFolder, PluginName + ".json");
		public string PluginIcon => Path.Combine(PluginFolder, "icon.png");

		internal bool isValidPlugin()
		{
			if (File.Exists(PluginZip) && File.Exists(PluginJson)) return true;
			return false;
		}
	}
}