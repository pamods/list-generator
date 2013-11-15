using System;
using System.Collections.Generic;
using System.Linq;

namespace ListGenerator
{
	internal class Mod
	{
		public string FileName { get; set; }
		public DateTime Date { get; set; }

		#region From ini file

		public string Name;
		public string Author;
		public string Link;
		public string Category;
		public string Version;
		public string Build;
		public string Description;

		#endregion

		#region Generated

		public string ShortName
		{
			get { return FileName.Substring(5, FileName.IndexOf('_') - 5); }
		}

		public string Url
		{
			get { return Program.BaseUrl + FileName.Substring(5); }
		}

		public string DateString
		{
			get { return Date.ToString("yyyy/MM/dd"); }
		}

		#endregion

		private Mod(string zipFileName, Dictionary<string, string> values)
		{
			FileName = zipFileName;

			values.TryGetValue("name", out Name);
			values.TryGetValue("author", out Author);
			values.TryGetValue("link", out Link);
			values.TryGetValue("category", out Category);
			values.TryGetValue("version", out Version);
			values.TryGetValue("build", out Build);
			values.TryGetValue("description", out Description);

			if (Link == null)
				Console.WriteLine("Warning: {0} has no Link", zipFileName);
			if (Description == null)
				Console.WriteLine("Warning: {0} has no Description", zipFileName);
		}

		public static Mod ParseIniFile(string zipFileName, string iniFileContents)
		{
			var lines = iniFileContents.Split(new[] { '\r', '\n', '\0' }, StringSplitOptions.RemoveEmptyEntries);

			if (lines[0] != "[PAMM]")
				throw new Exception("First line of ini file is not '[PAMM]'");

			var values = new Dictionary<string, string>();

			foreach (var line in lines.Skip(1))
			{
				var equalLocation = line.IndexOf('=');
				if (equalLocation > 0)
				{
					var key = line.Substring(0, equalLocation).ToLower();
					var value = line.Substring(equalLocation + 1);

					values.Add(key, value);
				}
			}

			return new Mod(zipFileName, values);
		}
	}
}