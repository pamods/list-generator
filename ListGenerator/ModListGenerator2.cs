using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ListGenerator
{
    class ModListGenerator2
    {
        private const string Filename = "modlist2.json";

        private List<string> Properties = new List<string>
        {
            "context",
            "identifier",
            "id", // deprecated
            "author",
            "version",
            "build",
            "date",
            "display_name",
            "display_name_de",
            "display_name_fr",
            "display_name_nl",
            "description",
            "description_de",
            "description_fr",
            "description_nl",
            "requires",
            "forum",
            "category",
            "icon",
        };

        private string BaseModUrl
        {
            get { return ConfigurationManager.AppSettings["baseModUrl"]; }
        }

        public void Refresh()
        {
            Console.WriteLine("Refresh {0}...", Filename);

            var modlist = LoadModList();

            WriteModList(modlist);
        }

        public void Add(string identifier, string url)
        {
            Console.WriteLine("Add '{0}' to {1}...", identifier, Filename);

            var client = new WebClient();
            var tempfile = Path.GetTempFileName();
            client.DownloadFile(url, tempfile);

            var modinfo = GetModInfo(tempfile, identifier);

            if(modinfo == null)
                throw new ApplicationException(String.Format("'{0}' not found in this mod archive", identifier));

            File.Delete(tempfile);

            modinfo = CleanupModInfo(modinfo);
            modinfo["url"] = url;

            var modlist = LoadModList();
            modlist = RemoveMod(modlist, identifier);

            modlist.Add(modinfo);

            WriteModList(modlist);

            Console.WriteLine("1 entry added");
        }

        public void Remove(string identifier)
        {
            Console.WriteLine("Remove '{0}' from {1}...", identifier, Filename);

            var found = false;

            var modlist = LoadModList();
            var nbentries = modlist.Count;

            modlist = RemoveMod(modlist, identifier);

            if (nbentries == modlist.Count)
                throw new ApplicationException(String.Format("'{0}' not found.", identifier));

            WriteModList(modlist);
        }

        private List<JObject> LoadModList()
        {
            var modlist = new List<JObject>();

            modlist.AddRange(LoadExternalMods());
            modlist.AddRange(LoadLocalMods("client", "user_mods"));
            modlist.AddRange(LoadLocalMods("server", "server_mods"));

            return modlist;
        }

        private List<JObject> LoadExternalMods()
        {
            var modlist = new List<JObject>();

            using (var jsonreader = new JsonTextReader(new StreamReader(Filename)))
            {
                var jmodlist = JToken.ReadFrom(jsonreader) as JArray;
                if (jmodlist != null)
                {
                    foreach (JObject modinfo in jmodlist)
                    {
                        string url = null;
                        if(modinfo["url"] != null)
                            url = modinfo["url"].Value<string>();

                        if (url == null || !url.StartsWith(BaseModUrl))
                        {
                            modlist.Add(modinfo);
                        }
                    }
                }
            }

            return modlist;
        }

        private List<JObject> LoadLocalMods(string context, string moddir)
        {
            var modlist = new List<JObject>();

            if (!Directory.Exists(moddir))
                return modlist;

            foreach (var modfile in Directory.GetFiles(moddir, "*.zip"))
            {
                string filename = Path.GetFileName(modfile);

                try
                {
                    var modinfo = GetModInfo(modfile, null); ;

                    if (modinfo == null || modinfo["context"] == null || !modinfo["context"].Value<String>().Equals(context))
                    {
                        Console.WriteLine("No '{0}' mod found in '{1}'.", context, filename);
                        continue;
                    }

                    modinfo = CleanupModInfo(modinfo);

                    //Add download url
                    modinfo.Add("url", BaseModUrl + moddir + "/" + filename);

                    modlist.Add(modinfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed reading {0}, Error: {1}", modfile, ex);
                }
            }

            return modlist;
        }

        private JObject GetModInfo(string modfile, string identifier)
        {
            using (var zipfile = new ZipFile(modfile))
            {
                foreach (ZipEntry entry in zipfile)
                {
                    if (entry.Name == "modinfo.json" || entry.Name.EndsWith("/modinfo.json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (var jsonreader = new JsonTextReader(new StreamReader(zipfile.GetInputStream(entry))))
                        {
                            var modinfo = JToken.ReadFrom(jsonreader) as JObject;
                            if (identifier == null || modinfo["identifier"].Value<string>() == identifier)
                            {
                                return modinfo;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private JObject CleanupModInfo(JObject modinfo)
        {
            var modinfo2 = new JObject();
            foreach (var property in modinfo.Properties())
            {
                if (Properties.Contains(property.Name))
                {
                    modinfo2.Add(property);
                }
            }
            return modinfo2;
        }

        private List<JObject> RemoveMod(List<JObject> modlist, string identifier)
        {
            var nbentries = modlist.Count;
            var modlist2 = new List<JObject>();

            foreach (var mod in modlist)
            {
                if (mod["identifier"].Value<string>() == identifier)
                {
                    string url = null;
                    if (mod["url"] != null)
                        url = mod["url"].Value<string>();

                    if (url != null && url.StartsWith(BaseModUrl))
                    {
                        url.Replace('\\','/'); // safety ...
                        var modfile = url.Substring(url.LastIndexOf("/") + 1);
                        var context = mod["context"].Value<string>();
                        var moddir = "user_mods";
                        if (context == "server")
                            moddir = "server_mods";

                        modfile = Path.Combine(moddir, modfile);

                        if (File.Exists(modfile))
                        {
                            File.Delete(modfile);
                            Console.WriteLine("Deleted {0}", modfile);
                        }
                    }

                    continue;
                }
                modlist2.Add(mod);
            }

            Console.WriteLine("{0} entry removed", nbentries - modlist2.Count);

            return modlist2;
        }

        private void WriteModList(List<JObject> modlist)
        {
            modlist.Sort(new MostListComparer());

            using (var sw = new StreamWriter(Filename))
            {
                sw.Write(JsonConvert.SerializeObject(modlist, Formatting.Indented));
            }

            Console.WriteLine("{0} has been written", Filename);
        }

        private class MostListComparer : IComparer<JObject>
        {
            public int Compare(JObject x, JObject y) {
                var result = String.Compare(x["context"].Value<String>(), y["context"].Value<String>(), StringComparison.Ordinal);
                
                if (result != 0)
                    return result;

                return String.Compare(x["identifier"].Value<String>(), y["identifier"].Value<String>(), StringComparison.Ordinal);
            }
        }
    }
}
