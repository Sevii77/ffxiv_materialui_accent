using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dalamud.Logging;

namespace Aetherment.Util {
	internal class GitHub {
		internal struct RepoInfo {
			public string author;
			public string repo;
			public string branch;
			
			public RepoInfo(string author, string repo, string branch) {
				this.author = author;
				this.repo = repo;
				this.branch = branch;
			}
		}
		
		private struct Repo {
			public struct RepoFile {
				public string path;
				public string type;
				public string sha;
				public uint size = 0;
			}
			
			public string sha;
			public RepoFile[] tree;
		}
		
		private static Dictionary<RepoInfo, Dir> repoCache = new();
		private static HttpClient httpClient;
		
		public static async Task<Dir> GetRepo(string info) {
			string[] seg = info.Split("/");
			return await GetRepo(new RepoInfo(seg[0], seg[1], seg.Length > 2 ? seg[2] : "master"));
		}
		
		public static async Task<Dir> GetRepo(RepoInfo info) {
			if(!repoCache.ContainsKey(info)) {
				Repo repo = JsonConvert.DeserializeObject<Repo>(await httpClient.GetStringAsync($"https://api.github.com/repos/{info.author}/{info.repo}/git/trees/{info.branch}?recursive=1"));
				Dir dir = new Dir("", repo.sha);
				
				foreach(var node in repo.tree) {
					Dir curdir = dir;
					string[] path = node.path.ToLower().Split("/");
					
					if(node.type == "tree") {
						for(int i = 0; i < path.Length - 1; i++)
							curdir = curdir.dirs[path[i]];
						
						string n = path[path.Length - 1];
						curdir.AddDir(n, node.sha);
					} if(node.type == "blob") {
						for(int i = 0; i < path.Length - 1; i++)
							curdir = curdir.dirs[path[i]];
						
						curdir.AddFile(path[path.Length - 1], node.sha, $"https://raw.githubusercontent.com/{info.author}/{info.repo}/{info.branch}/{node.path}");
					}
				}
				
				lock(repoCache) {
					repoCache[info] = dir;
				}
			}
			
			return repoCache[info];
		}
		
		public static Task<string> GetString(string url) {
			return httpClient.GetStringAsync(url);
		}
		
		public static Task<byte[]> GetByteArray(string url) {
			return httpClient.GetByteArrayAsync(url);
		}
		
		public static void ClearCache() {
			if(httpClient == null) {
				var handler = new HttpClientHandler();
				handler.Proxy = null;
				handler.UseProxy = false;
				
				httpClient = new HttpClient(handler);
				httpClient.DefaultRequestHeaders.Add("User-Agent", "FFXIV-Aetherment");
			}
			
			lock(repoCache) {
				repoCache.Clear();
			}
		}
	}
}