using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
		
		private static Dictionary<RepoInfo, Dir> repoCache = new();
		private static HttpClient httpClient;
		
		public static async Task<Dir> GetRepo(string info) {
			string[] seg = info.Split("/");
			return await GetRepo(new RepoInfo(seg[0], seg[1], seg.Length > 2 ? seg[2] : "master"));
		}
		
		public static async Task<Dir> GetRepo(RepoInfo info) {
			if(!repoCache.ContainsKey(info)) {
				dynamic repo = JsonConvert.DeserializeObject(await httpClient.GetStringAsync($"https://api.github.com/repos/{info.author}/{info.repo}/git/trees/{info.branch}?recursive=1"));
				Dir dir = new Dir("", repo?.sha);
				
				foreach(var file in repo?.tree) {
					Dir curdir = dir;
					string[] path = file.path.Split("/");
					
					if(file.type == "tree") {
						for(int i = 0; i < path.Length - 1; i++)
							curdir = curdir.dirs[path[i]];
						
						string n = path[path.Length - 1];
						curdir.AddDir(n, file.sha);
						// curdir.dirs[n] = new Dir(n, file.sha);
					} if(file.type == "blob") {
						for(int i = 0; i < path.Length - 1; i++)
							curdir = curdir.dirs[path[i]];
						
						curdir.AddFile(path[path.Length - 1], file.sha, $"https://raw.githubusercontent.com/{info.author}/{info.repo}/{info.branch}/{file.path}");
						// curdir.files[path[path.Length - 1]] = (file.sha, $"https://raw.githubusercontent.com/{info.author}/{info.repo}/{info.branch}/{file.path}";
					}
				}
				
				lock(repoCache) {
					repoCache[info] = dir;
				}
			}
			
			return repoCache[info];
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