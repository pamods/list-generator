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

					res.Add(Mod.ParseIniFile(filename, iniFileContents));
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

		private void GenerateIniFile(List<Mod> modList)
		{
			Render.FileToFile("templates/ini.txt", new { mods = modList }, "modlist.ini", new RenderContextBehaviour { RaiseExceptionOnDataContextMiss = false, RaiseExceptionOnEmptyStringValue = false });
		}
	}
}
