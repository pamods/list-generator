using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ListGenerator
{
	internal class Mod
	{
		public string FileName { get; set; }

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
		public string icon { get; set; }
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

		public string Url
		{
			get { return Program.BaseModUrl + FileName; }
		}

		#endregion
	}
}
