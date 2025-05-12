using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DalamudPluginServer
{
	internal class PluginManager
	{
		private static readonly string RepoFolder = "repos";
		private string ReposRoot { get { return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), RepoFolder); } }
		private List<string> PluginRepos { get; set; } = new List<string>();

		public PluginManager()
		{
			if (!Directory.Exists(ReposRoot)) Directory.CreateDirectory(ReposRoot);

			bool isGitAvailable = CheckForGit().GetAwaiter().GetResult();
			if (!isGitAvailable)
			{
				Console.WriteLine("[!] Git command not found.");
				throw new InvalidOperationException("Git command not found.");
			}
		}

		private async Task<bool> CheckForGit()
		{
			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = "--version",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				using (var process = new Process { StartInfo = startInfo })
				{
					process.Start();

					string output = await process.StandardOutput.ReadToEndAsync();
					string error = await process.StandardError.ReadToEndAsync();

					await Task.Run(() => process.WaitForExit());

					if (process.ExitCode == 0)
					{
						Console.WriteLine($"Git found: {output.Trim()}");
						return true;
					}
					else
					{
						Console.WriteLine($"Git error: {error.Trim()}");
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[!] Exception checking for Git: {ex.Message}");
				return false;
			}
		}

		internal void AddRepo(params string[] repoUrls)
		{
			PluginRepos.AddRange(repoUrls);
		}

		public async Task DownloadOrUpdateRepositoriesAsync()
		{
			var tasks = new List<Task>();

			foreach (var repoUrl in PluginRepos)
			{
				tasks.Add(Task.Run(async () =>
				{
					string repoName = GetRepoNameFromUrl(repoUrl);
					string destinationPath = Path.Combine(ReposRoot, repoName);

					if (!Directory.Exists(destinationPath))
					{
						Console.WriteLine($"[*] Cloning '{repoName}'...");
						await CloneRepoAsync(repoUrl, destinationPath);
					}
					else
					{
						Console.WriteLine($"[*] Updating '{repoName}'...");
						await PullRepoAsync(destinationPath);
					}
				}));
			}

			await Task.WhenAll(tasks);
		}

		private async Task CloneRepoAsync(string repoUrl, string destinationPath)
		{
			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = $"clone {repoUrl} \"{destinationPath}\"",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				using (var process = new Process { StartInfo = startInfo })
				{
					process.Start();
					string output = await process.StandardOutput.ReadToEndAsync();
					string error = await process.StandardError.ReadToEndAsync();

					await Task.Run(() => process.WaitForExit());

					if (process.ExitCode == 0)
					{
						Console.WriteLine($"[*] Successfully cloned '{repoUrl}'.");
					}
					else
					{
						Console.WriteLine($"[!] Failed to clone '{repoUrl}': {error}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[!] Exception cloning '{repoUrl}': {ex.Message}");
			}
		}

		private async Task PullRepoAsync(string repoPath)
		{
			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = "pull",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					WorkingDirectory = repoPath
				};

				using (var process = new Process { StartInfo = startInfo })
				{
					process.Start();
					string output = await process.StandardOutput.ReadToEndAsync();
					string error = await process.StandardError.ReadToEndAsync();

					await Task.Run(() => process.WaitForExit());

					if (process.ExitCode == 0)
					{
						Console.WriteLine($"[*] Successfully updated '{Path.GetFileName(repoPath)}'.");
					}
					else
					{
						Console.WriteLine($"[!] Failed to update '{Path.GetFileName(repoPath)}': {error}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[!] Exception updating '{Path.GetFileName(repoPath)}': {ex.Message}");
			}
		}

		private string GetRepoNameFromUrl(string repoUrl)
		{
			string trimmed = repoUrl.TrimEnd('/');
			string lastSegment = trimmed.Substring(trimmed.LastIndexOf('/') + 1);
			if (lastSegment.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
			{
				lastSegment = lastSegment.Substring(0, lastSegment.Length - 4);
			}
			return lastSegment;
		}

		internal async Task BuildRepos()
		{
			var repoDirs = Directory.GetDirectories(ReposRoot);

			foreach (var repoDir in repoDirs)
			{
				string slnPath = FindSolutionFile(repoDir);
				if (slnPath != null)
				{
					Console.WriteLine($"[*] Building solution: {slnPath}");
					bool buildSuccess = await BuildSolutionAsync(slnPath);

					if (buildSuccess)
					{
						Console.WriteLine($"[*] Build succeeded: {Path.GetFileName(repoDir)}");
					}
					else
					{
						Console.WriteLine($"[!] Build failed: {Path.GetFileName(repoDir)}");
					}
				}
				else
				{
					Console.WriteLine($"[*] No .sln file found in {Path.GetFileName(repoDir)}, skipping.");
				}
			}
		}

		private string FindSolutionFile(string repoDir)
		{
			var slnFiles = Directory.GetFiles(repoDir, "*.sln", SearchOption.TopDirectoryOnly);
			return slnFiles.FirstOrDefault(); // If multiple .sln, pick the first one
		}

		private async Task<bool> BuildSolutionAsync(string solutionPath)
		{
			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = "dotnet",
					Arguments = $"build \"{solutionPath}\" --configuration Release",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				using (var process = new Process { StartInfo = startInfo })
				{
					process.Start();
					string output = await process.StandardOutput.ReadToEndAsync();
					string error = await process.StandardError.ReadToEndAsync();

					await Task.Run(() => process.WaitForExit());

					if (process.ExitCode == 0)
					{
						return true;
					}
					else
					{
						Console.WriteLine($"[!] Build error: {error}");
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[!] Exception building solution: {ex.Message}");
				return false;
			}
		}

		internal async Task CopyAllPluginFiles(string newFolder)
		{
			var repoDirs = Directory.GetDirectories(ReposRoot);
			foreach (var repoDir in repoDirs)
			{
				CopySinglePluginFiles(repoDir, newFolder);
			}
		}

		private void CopySinglePluginFiles(string repoDir, string destinationFolder)
		{
			var buildDir = Path.Combine(repoDir, "bin", "x64", "Release");

			var folderName = Directory.EnumerateDirectories(repoDir).Select(Path.GetFileName).FirstOrDefault(name => name != null && !name.Contains("."));

			if (folderName == null)
			{
				throw new Exception("Unable to locate plugin folder!");
			}


			var sourceDir = Path.Combine(repoDir, folderName, "bin", "x64", "Release", folderName);
			var dir = new DirectoryInfo(sourceDir);

			if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			var outputFolder = Path.Combine(destinationFolder, folderName);
			// If the destination directory doesn't exist, create it
			Directory.CreateDirectory(outputFolder);

			// Copy all the files
			foreach (var file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(outputFolder, file.Name);
				Console.WriteLine($"[*] Copying {file} to {targetFilePath}");
				file.CopyTo(targetFilePath, overwrite: true);
			}
		}
	}
}