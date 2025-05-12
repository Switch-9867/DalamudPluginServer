using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DalamudPluginServer
{
	internal class Program
	{
		static PluginDatabase PluginDatabase { get; set; }
		static WebServer WebServer { get; set; }
		static PluginFileManager PluginManager { get; set; }

		static async Task Main(string[] args)
		{
			string[] repoUrls = ApplicationInI.GetRepoUrls();

			PluginDatabase = new PluginDatabase();

			PluginManager = new PluginFileManager();
			PluginManager.AddRepo(repoUrls);
			await PluginManager.DownloadOrUpdateRepositoriesAsync();
			await PluginManager.BuildRepos();
			await PluginManager.CopyAllPluginFiles(PluginDatabase.PluginsRoot);

			WebServer = new WebServer(3000);

			WebServer.pluginDatabase = PluginDatabase;
			WebServer.Start();

			Console.WriteLine("Press any key to close.");
			Console.ReadKey();
		}
	}
}
