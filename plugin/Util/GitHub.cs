using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Logging;

namespace Aetherment.Util {
	public class GitHub {
		public struct RepoInfo {
			public string Author;
			public string Repo;
			public string Branch;
			
			public RepoInfo(string author, string repo, string branch) {
				Author = author;
				Repo = repo;
				Branch = branch;
			}
		}
		
		private struct Repo {
			public struct RepoFile {
				public string Path;
				public string Type;
				public string Sha;
				public int Size = 0;
			}
			
			public string Sha;
			public RepoFile[] Tree;
		}
		
		private static Dictionary<RepoInfo, Dir> repoCache = new();
		
		public static async Task<Dir> GetRepo(string info) {
			string[] seg = info.Split("/");
			return await GetRepo(new RepoInfo(seg[0], seg[1], seg.Length > 2 ? seg[2] : "master"));
		}
		
		public static async Task<Dir> GetRepo(RepoInfo info) {
			if(info.Author == "")
				return null;
			
			if(!repoCache.ContainsKey(info)) {
				Repo repo = JsonConvert.DeserializeObject<Repo>(await Installer.GetString($"https://api.github.com/repos/{info.Author}/{info.Repo}/git/trees/{info.Branch}?recursive=1"));
				Dir dir = new Dir("", repo.Sha);
				
				foreach(var node in repo.Tree) {
					Dir curdir = dir;
					string[] path = node.Path.ToLower().Split("/");
					
					if(node.Type == "tree") {
						for(int i = 0; i < path.Length - 1; i++)
							curdir = curdir.Dirs[path[i]];
						
						string n = path[path.Length - 1];
						curdir.AddDir(n, node.Sha);
					} if(node.Type == "blob") {
						for(int i = 0; i < path.Length - 1; i++)
							curdir = curdir.Dirs[path[i]];
						
						curdir.AddFile(path[path.Length - 1], node.Sha, $"https://raw.githubusercontent.com/{info.Author}/{info.Repo}/{info.Branch}/{node.Path}", node.Size);
					}
				}
				
				lock(repoCache) {
					repoCache[info] = dir;
				}
			}
			
			return repoCache[info];
		}
		
		public static void ClearCache() {
			lock(repoCache) {
				repoCache.Clear();
			}
		}
	}
}