using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ListGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			new Program().Run();
		}

		public static string BaseModUrl
		{
			get { return ConfigurationManager.AppSettings["baseModUrl"]; }
		}

		private void Run()
		{
			var modList = LoadMods();

			GenerateJSONFileNewtonSoft(modList);
		}

		private List<JObject> LoadMods()
		{
			var res = new List<JObject>();

			foreach (var filename in Directory.EnumerateFiles("user_mods", "*.zip"))
			{
				try
				{
					var zip = new ZipFile(filename);
					var zipJSONFile = FindJSONFile(zip);

					if (zipJSONFile == null)
						throw new Exception("Unable to find modinfo.json file");

					var stream = zip.GetInputStream(zipJSONFile);
					var memoryStream = new MemoryStream();
					StreamUtils.Copy(stream, memoryStream, new byte[4196]);

					var jsonFileContents = Encoding.UTF8.GetString(memoryStream.GetBuffer());
					JObject mod = JObject.Parse(jsonFileContents);

					//Add missing things
					mod.Add("url", BaseModUrl + new FileInfo(filename).Name);

					//Remove things we dont want
					mod.Remove("start");
					mod.Remove("new_game");
					mod.Remove("lobby");
					mod.Remove("live_game");
					mod.Remove("settings");
					mod.Remove("system_editor");
					mod.Remove("global_mod_list");
					mod.Remove("server_browser");
					mod.Remove("social");
					mod.Remove("game_over");

					mod.Remove("priority");
					mod.Remove("enabled");
					mod.Remove("context");
					mod.Remove("identifier");
					mod.Remove("signature");

					res.Add(mod);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Failed reading {0}, Error: {1}", filename, ex);
				}
			}

			return res;
		}

		private ZipEntry FindJSONFile(ZipFile zip)
		{
			foreach (ZipEntry entry in zip)
			{
				if (entry.Name.EndsWith("/modinfo.json", StringComparison.InvariantCultureIgnoreCase))
				{
					return entry;
				}
			}
			return null;
		}

		private void GenerateJSONFileNewtonSoft(List<JObject> modList)
		{
			using (StreamWriter sw = new StreamWriter("modlist.json"))
			{
				var dict = new SortedDictionary<string, JObject>();
				foreach (var mod in modList)
				{
					dict.Add((string)mod.GetValue("id"), mod);
					mod.Remove("id");
				}

				sw.Write(JsonConvert.SerializeObject(dict, Formatting.Indented));
				sw.Close();
			}
		}
	}
}
