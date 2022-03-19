using System.Collections.Generic;

namespace Aetherment.Util {
	public struct PMeta {
		public int FileVersion = 0;
		public string Name = "No Name";
		public string Author = "Unknown";
		public string Description = "";
		public string Version = "";
		public string Website = "";
		public Dictionary<string, string> FileSwaps = new();
		public Dictionary<string, PGroup> Groups = new();
		
		public struct PGroup {
			public string GroupName = "noname";
			public string SelectionType = "single";
			public List<POption> Options = new();
			
			public struct POption {
				public string OptionName = "noname";
				public string OptionDesc = "";
				public Dictionary<string, string[]> OptionFiles = new();
			}
		}
	}
}