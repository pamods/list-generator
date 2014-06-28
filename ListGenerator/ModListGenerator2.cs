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

        private string BaseModUrl
        {
            get { return ConfigurationManager.AppSettings["baseModUrl"]; }
        }

        public void Refresh()
        {
            Console.WriteLine("Refresh {0}...", Filename);

            var modlist = LoadModList();

            FixDependencies(modlist);
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

            modinfo.url = url;

            var modlist = LoadModList();
            modlist = RemoveMod(modlist, identifier);

            modlist.Add(modinfo);

            FixDependencies(modlist);
            WriteModList(modlist);

            Console.WriteLine("1 entry added");
        }

        public void Remove(string identifier)
        {
            Console.WriteLine("Remove '{0}' from {1}...", identifier, Filename);

            var modlist = LoadModList();
            var nbentries = modlist.Count;

            modlist = RemoveMod(modlist, identifier);

            if (nbentries == modlist.Count)
                throw new ApplicationException(String.Format("'{0}' not found.", identifier));

            FixDependencies(modlist);
            WriteModList(modlist);
        }

        private List<ModInfo> LoadModList()
        {
            var modlist = new List<ModInfo>();

            modlist.AddRange(LoadExternalMods());
            modlist.AddRange(LoadLocalMods("client", "user_mods"));
            modlist.AddRange(LoadLocalMods("server", "server_mods"));

            return modlist;
        }

        private List<ModInfo> LoadExternalMods()
        {
            var modlist = new List<ModInfo>();

            using (var reader = new StreamReader(Filename))
            {
                var json = reader.ReadToEnd();

                var oldlist = JsonConvert.DeserializeObject<List<ModInfo>>(json);

                if (oldlist != null)
                {
                    foreach (ModInfo modinfo in oldlist)
                    {
                        if (modinfo.url == null || !modinfo.url.StartsWith(BaseModUrl))
                        {
                            modlist.Add(modinfo);
                        }
                    }
                }
            }

            return modlist;
        }

        private List<ModInfo> LoadLocalMods(string context, string moddir)
        {
            var modlist = new List<ModInfo>();

            if (!Directory.Exists(moddir))
                return modlist;

            foreach (var modfile in Directory.GetFiles(moddir, "*.zip"))
            {
                string filename = Path.GetFileName(modfile);

                try
                {
                    var modinfo = GetModInfo(modfile, null); ;

                    if (modinfo == null || context != modinfo.context)
                    {
                        Console.WriteLine("No '{0}' mod found in '{1}'.", context, filename);
                        continue;
                    }

                    //download url
                    modinfo.url = BaseModUrl + moddir + "/" + filename;

                    modlist.Add(modinfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed reading {0}, Error: {1}", modfile, ex);
                }
            }

            return modlist;
        }

        private ModInfo GetModInfo(string modfile, string identifier)
        {
            using (var zipfile = new ZipFile(modfile))
            {
                foreach (ZipEntry entry in zipfile)
                {
                    if (entry.Name == "modinfo.json" || entry.Name.EndsWith("/modinfo.json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (var reader = new StreamReader(zipfile.GetInputStream(entry)))
                        {
                            var json = reader.ReadToEnd();
                            var modinfo = JsonConvert.DeserializeObject<ModInfo>(json);

                            if (identifier == null || modinfo.identifier == identifier)
                            {
                                return modinfo;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private List<ModInfo> RemoveMod(List<ModInfo> modlist, string identifier)
        {
            var nbentries = modlist.Count;
            var modlist2 = new List<ModInfo>();

            foreach (var mod in modlist)
            {
                if (mod.identifier == identifier)
                {
                    if (mod.url != null && mod.url.StartsWith(BaseModUrl))
                    {
                        mod.url = mod.url.Replace('\\', '/'); // safety ...
                        var modfile = mod.url.Substring(mod.url.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        var context = mod.context;
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

        private void FixDependencies(List<ModInfo> modlist)
        {
            var compatibility = new Dictionary<String,String>();

            foreach(var mod in modlist)
            {
                var id = mod.id ?? mod.identifier;
                compatibility[id] = mod.identifier;
            }

            foreach(var mod in modlist)
            {
                if(mod.requires != null) {
                    mod.dependencies = new List<string>();
                    foreach(var dependency in mod.requires)
                    {
                        string id;
                        if(compatibility.TryGetValue(dependency, out id))
                            mod.dependencies.Add(id);
                        else
                            mod.dependencies.Add(dependency);
                    }
                    mod.requires = null;
                }
            }
        }

        private void WriteModList(List<ModInfo> modlist)
        {
            modlist.Sort(new MostListComparer());

            using (var sw = new StreamWriter(Filename))
            using (var jw = new JsonTextWriter(sw))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Formatting = Formatting.Indented;

                serializer.Serialize(jw, modlist);
            }

            Console.WriteLine("{0} has been written", Filename);
        }

        private class MostListComparer : IComparer<ModInfo>
        {
            public int Compare(ModInfo x, ModInfo y)
            {
                var result = String.Compare(x.context, y.context, StringComparison.Ordinal);
                
                if (result != 0)
                    return result;

                return String.Compare(x.identifier, y.identifier, StringComparison.Ordinal);
            }
        }
    }

    class ModInfo
    {
        // Uber
        public string context { get; set; }
        public string identifier { get; set; }
        public string author { get; set; }
        public string version { get; set; }
        public string display_name { get; set; }
        public string description { get; set; }
        public List<string> dependencies { get; set; }
        //public string signature { get; set; }
        //public bool enabled { get; set; }
        
        // PAMM
        public string build { get; set; }
        public string date { get; set; }
        public string display_name_de { get; set; }
        public string display_name_fr { get; set; }
        public string display_name_nl { get; set; }
        public string description_de { get; set; }
        public string description_fr { get; set; }
        public string description_nl { get; set; }
        public string forum { get; set; }
        public List<string> category { get; set; }
        public string icon { get; set; }
        public string url { get; set; }
        
        // deprecated
        public string id { get; set; }
        public string[] requires { get; set; }
    }
}
