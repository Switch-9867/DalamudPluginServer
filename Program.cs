using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DalamudPluginServer
{
	internal class Program
	{
		static void Main(string[] args)
		{
			PluginDatabase plugins = new PluginDatabase("plugins");

			WebServer ws = new WebServer(3000);

			ws.pluginDatabase = plugins;
			ws.Start();

			Console.WriteLine("Press any key to close.");
			Console.ReadKey();
		}
	}
}
