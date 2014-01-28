﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

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

			modList = modList.OrderBy(x => x.identifier).ToList();

			GenerateJSONFileNewtonSoft(modList);
		}

		private List<Mod> LoadMods()
		{
			var res = new List<Mod>();

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

					var nmod = JsonConvert.DeserializeObject<Mod>(jsonFileContents);
					nmod.FileName = new FileInfo(filename).Name;
					res.Add(nmod);
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

		private DateTime GetLatestModifiedTime(ZipFile zip)
		{
			DateTime time = DateTime.MinValue;
			foreach (ZipEntry entry in zip)
			{
				if (entry.DateTime > time)
					time = entry.DateTime;
			}
			return time.ToUniversalTime();
		}
		
		private void GenerateJSONFileNewtonSoft(List<Mod> modList)
		{
			StringBuilder sb = new StringBuilder();
			StreamWriter sw = new StreamWriter("modlist.json");

			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				writer.Formatting = Formatting.Indented;
				writer.WriteStartObject();
				foreach (var mod in modList)
				{
					writer.WritePropertyName(mod.id);
					writer.WriteStartObject();
					writer.WritePropertyName("display_name");
					writer.WriteValue(mod.display_name);
					writer.WritePropertyName("description");
					writer.WriteValue(mod.description);
					writer.WritePropertyName("author");
					writer.WriteValue(mod.author);
					writer.WritePropertyName("version");
					writer.WriteValue(mod.version);
					writer.WritePropertyName("build");
					writer.WriteValue(mod.build);
					writer.WritePropertyName("date");
					writer.WriteValue(mod.date);
					writer.WritePropertyName("forum");
					writer.WriteValue(mod.forum);
					writer.WritePropertyName("url");
					writer.WriteValue(mod.Url);
					if (mod.icon != null)
					{
						writer.WritePropertyName("icon");
						writer.WriteValue(mod.icon);
					}
					if (mod.category != null)
					{
						writer.WritePropertyName("category");
						writer.WriteStartArray();
						foreach (var s in mod.category)
						{
							writer.WriteValue(s);
						}
						writer.WriteEnd();
					}
					if (mod.requires != null)
					{
						writer.WritePropertyName("requires");
						writer.WriteStartArray();
						foreach (var s in mod.requires)
						{
							writer.WriteValue(s);
						}
						writer.WriteEnd();
					}
					writer.WriteEndObject();

				}
				writer.WriteEndObject();
			}
		}
	}
}
