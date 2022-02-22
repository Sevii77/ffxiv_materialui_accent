using System.Collections.Generic;

namespace Aetherment.Util {
	public class Dir {
		public struct File {
			public string Name;
			public string Sha;
			public string Path;
			public int Size;
			
			public File(string name, string sha, string path, int size) {
				Name = name;
				Sha = sha;
				Path = path;
				Size = size;
			}
		}
		
		public string Name;
		public string Sha;
		public Dictionary<string, File> Files;
		public Dictionary<string, Dir> Dirs;
		
		public Dir(string name, string sha) {
			Name = name;
			Sha = sha;
			Files = new Dictionary<string, File>();
			Dirs = new Dictionary<string, Dir>();
		}
		
		public Dir GetPathDir(string path) {
			Dir dir = this;
			foreach(string subpath in path.Split("/")) {
				if(!dir.Dirs.ContainsKey(subpath))
					return null;
				
				dir = dir.Dirs[subpath];
			}
			
			return dir;
		}
		
		public Dir AddDir(string name, string sha) {
			Dir dir = new Dir(name, sha);
			Dirs[name] = dir;
			
			return dir;
		}
		
		public void AddFile(string name, string sha, string path, int size) {
			Files[name] = new File(name, sha, path, size);
		}
	}
}