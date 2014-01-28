using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ListGenerator
{
	internal class Mod
	{

		public string FileName { get; set; }
		public DateTime Date { get; set; }
		public string Icon;

		/* http://json2csharp.com/ */
		#region From json file
		public string context { get; set; }
		public string identifier { get; set; }
		public string display_name { get; set; }
		public string description { get; set; }
		public string author { get; set; }
		public string version { get; set; }
		public string build { get; set; }
		public string date { get; set; }
		public string signature { get; set; }
		public string forum { get; set; }
		public List<string> category { get; set; }
		public string id { get; set; }
		public int priority { get; set; }
		public List<string> live_game { get; set; }
		public List<string> settings { get; set; }
		public List<string> global_mod_list { get; set; }
		public List<string> requires { get; set; }
		public bool enabled { get; set; }

		#endregion

		#region Generated

		public string ShortName
		{
			get { return FileName.Substring(0, FileName.LastIndexOf('_')); }
		}

		public string Url
		{
			get { return Program.BaseNewModUrl + FileName; }
		}

		public string DateString
		{
			get { return Date.ToString("yyyy/MM/dd"); }
		}

		public string IconUrl
		{
			get { return Program.BaseIconsUrl + Icon; }
		}


		public List<Dictionary<string, string>> requiresToString
		{
			get
			{
				var reqList = new List<Dictionary<string, string>>();
				if (requires != null)
				{
					for (var i = 0; i < requires.Count; i++)
					{
						var dict = new Dictionary<string, string>();
						dict["name"] = requires[i];
						if (i + 1 != requires.Count)
						{
							dict["comma"] = ",";
						}
						reqList.Add(dict);
					}
				}
				return reqList;
			}
		}

		public List<Dictionary<string, string>> categoryToString
		{
			get
			{
				var catList = new List<Dictionary<string, string>>();
				if (category != null)
				{
					for (var i = 0; i < category.Count; i++)
					{
						var dict = new Dictionary<string, string>();
						dict["name"] = category[i];
						if (i + 1 != category.Count)
						{
							dict["comma"] = ",";
						}
						catList.Add(dict);
					}
				}
				return catList;
			}
		}
		#endregion
	}
}
