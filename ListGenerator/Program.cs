using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Nustache.Core;
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

        public static string BaseNewModUrl
        {
            get { return ConfigurationManager.AppSettings["baseNewModUrl"]; }
        }

		public static string BaseIconsUrl
		{
			get { return ConfigurationManager.AppSettings["baseIconUrl"]; }
		}

		private void Run()
		{
            
			var modList = LoadMods();

			ApplyDescriptionUnderrides(modList);

			modList = modList.OrderBy(x => x.ShortName).ToList();

            GenerateIniFile(modList);
            
            var nmodList = LoadnMods();

            nmodList = nmodList.OrderBy(x => x.identifier).ToList();

            GenerateJSONFileNewtonSoft(nmodList);
			
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

					var mod = Mod.ParseIniFile(filename.Substring(5), iniFileContents);
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

        private List<nMod> LoadnMods()
        {
            var res = new List<nMod>();

            foreach (var filename in Directory.EnumerateFiles("user_mods", "*.zip"))
            {
                try
                {
                    var zip = new ZipFile(filename);
                    var zipJSONFile = FindJSONFile(zip);

                    if (zipJSONFile == null)
                        throw new Exception("Unable to find modinfo.json file, or multiple modinfo.json files found");

                    var stream = zip.GetInputStream(zipJSONFile);
                    var memoryStream = new MemoryStream();
                    StreamUtils.Copy(stream, memoryStream, new byte[4196]);

                    
                    var jsonFileContents = Encoding.UTF8.GetString(memoryStream.GetBuffer());

                    var nmod = JsonConvert.DeserializeObject<nMod>(jsonFileContents);
                    nmod.Date = GetLatestModifiedTime(zip);
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

        private void GenerateJSONFile(List<nMod> nmodList)
        {
            //not used anymore is a hassle to get the ending , right
            Render.FileToFile("templates/json.txt", new { mods = nmodList }, "modlist.json", new RenderContextBehaviour { RaiseExceptionOnDataContextMiss = false, RaiseExceptionOnEmptyStringValue = false });
        }

        private void GenerateJSONFileNewtonSoft(List<nMod> nmodList)
        {
            StringBuilder sb = new StringBuilder();
            StreamWriter sw = new StreamWriter("modlist.json");
            
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
              writer.Formatting = Formatting.Indented;
              writer.WriteStartObject();
              foreach(var nmod in nmodList)
              {
                  writer.WritePropertyName(nmod.id);
                  writer.WriteStartObject();
                  writer.WritePropertyName("display_name");
                  writer.WriteValue(nmod.display_name);
                  writer.WritePropertyName("description");
                  writer.WriteValue(nmod.description);
                  writer.WritePropertyName("author");
                  writer.WriteValue(nmod.author);
                  writer.WritePropertyName("version");
                  writer.WriteValue(nmod.version);
                  writer.WritePropertyName("build");
                  writer.WriteValue(nmod.build);
                  writer.WritePropertyName("date");
                  writer.WriteValue(nmod.DateString);
                  writer.WritePropertyName("forum");
                  writer.WriteValue(nmod.forum);
                  writer.WritePropertyName("url");
                  writer.WriteValue(nmod.Url);
                  if(nmod.category != null)
                  {
                      writer.WritePropertyName("category");
                      writer.WriteStartArray();
                      foreach (var s in nmod.category)
                      {
                          writer.WriteValue(s);
                      }
                      writer.WriteEnd();
                  }
                  if (nmod.requires != null)
                  {
                      writer.WritePropertyName("requires");
                      writer.WriteStartArray();
                      foreach (var s in nmod.requires)
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
