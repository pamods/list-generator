using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Nustache.Core;

namespace ListGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			new Program().Run();
		}

		public static string BaseUrl
		{
			get { return ConfigurationManager.AppSettings["baseUrl"]; }
		}

		private void Run()
		{
			var modList = LoadMods();

			ApplyDescriptionUnderrides(modList);

			GenerateIniFile(modList);
		}

		private List<Mod> LoadMods()
		{
			var res = new List<Mod>();

			foreach (var filename in Directory.EnumerateFiles("mods", "*.zip"))
			{
				try
				{
					var zip = new ZipFile(filename);
					var zipIniFile = FindIniFile(zip);

					if (zipIniFile == null)
						throw new Exception("Unable to find ini file, or multiple ini files found");

					var stream = zip.GetInputStream(zipIniFile);
					var memoryStream = new MemoryStream();
					StreamUtils.Copy(stream, memoryStream, new byte[4196]);

					var iniFileContents = Encoding.UTF8.GetString(memoryStream.GetBuffer());

					var mod = Mod.ParseIniFile(filename, iniFileContents);
					mod.Date = GetLatestModifiedTime(zip);
					res.Add(mod);
				}
				catch(Exception ex)
				{
					Console.WriteLine("Failed reading {0}, Error: {1}", filename, ex);
				}
			}

			return res;
		}

		private ZipEntry FindIniFile(ZipFile zip)
		{
			foreach (ZipEntry entry in zip)
			{
				if (entry.Name.EndsWith(".ini", StringComparison.InvariantCultureIgnoreCase))
				{
					return entry;
				}
			}
			return null;
		}

		private DateTime GetLatestModifiedTime(ZipFile zip)
		{
			DateTime time = DateTime.MinValue;
			foreach (ZipEntry entry in zip)
			{
				if (entry.DateTime > time)
					time = entry.DateTime;
			}
			return time;
		}

		/// <summary>
		/// Apply descriptions from file unless the mod already has a description
		/// </summary>
		/// <param name="modList"></param>
		private void ApplyDescriptionUnderrides(List<Mod> modList)
		{
			foreach (var line in File.ReadAllLines("templates/descriptions.txt").Where(x => x.Contains('=')))
			{
				var equalsIndex = line.IndexOf('=');
				var key = line.Substring(0, equalsIndex);
				var value = line.Substring(equalsIndex + 1);

				var mod = modList.FirstOrDefault(m => m.ShortName == key);
				if (mod != null && string.IsNullOrWhiteSpace(mod.Description))
					mod.Description = value;
			}
		}

		private void GenerateIniFile(List<Mod> modList)
		{
			Render.FileToFile("templates/ini.txt", new { mods = modList }, "modlist.ini", new RenderContextBehaviour { RaiseExceptionOnDataContextMiss = false, RaiseExceptionOnEmptyStringValue = false });
		}
	}
}
