using System.Collections.Generic;

namespace Aetherment.Util {
	internal class Dir {
		internal struct File {
			public string name;
			public string sha;
			public string path;
			
			public File(string name, string sha, string path) {
				this.name = name;
				this.sha = sha;
				this.path = path;
			}
		}
		
		public string name;
		public string sha;
		public Dictionary<string, File> files;
		public Dictionary<string, Dir> dirs;
		
		public Dir(string name, string sha) {
			this.name = name;
			this.sha = sha;
			files = new Dictionary<string, File>();
			dirs = new Dictionary<string, Dir>();
		}
		
		public Dir GetPathDir(string path) {
			Dir dir = this;
			foreach(string subpath in path.Split("/")) {
				if(!dir.dirs.ContainsKey(subpath))
					return null;
				
				dir = dir.dirs[subpath];
			}
			
			return dir;
		}
		
		public Dir AddDir(string name, string sha) {
			Dir dir = new Dir(name, sha);
			dirs[name] = dir;
			
			return dir;
		}
		
		public void AddFile(string name, string sha, string path) {
			files[name] = new File(name, sha, path);
		}
	}
}