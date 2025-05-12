using System;
using System.IO;
using System.Threading.Tasks;

namespace DalamudPluginServer
{
	internal class ApplicationInI
	{
		private static string IniFile { get { return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "repos.ini"); } }

		internal static string[] GetRepoUrls()
		{
			if (!File.Exists(IniFile)) File.WriteAllText(IniFile, "");
			string[] lines = File.ReadAllLines(IniFile);
			return lines;
		}
	}
}