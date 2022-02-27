using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

using Dalamud.Logging;

using Aetherment.Util;
using Aetherment.Format;

// TODO: file can not exists after download even tho it does, fix that

namespace Aetherment {
	internal class Installer {
		// public static string Status {get; private set;}
		private struct PMeta {
			public int FileVersion = 0;
			public string Name = "No Name";
			public string Author = "Unknown";
			public string Description = "";
			public string Version = "";
			public string Website = "";
			public Dictionary<int, int> FileSwaps = new(); // idk what the values are but we dont care
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
		
		public class Status {
			public bool Busy = false;
			public int Progress = 0;
			public int Total = 0;
			public string CurrentJob = "";
			public string CurrentJobDetails = "";
		}
		
		public static Status InstallStatus;
		private static HttpClient httpClient;
		private static List<Mod> downloadQueue;
		
		private static void StartDownloader() {
			Task.Run(async() => {
				while(downloadQueue.Count > 0) {
					InstallStatus.Busy = true;
					
					var mod = downloadQueue[0];
					var filesPath = $"{Penumbra.GetModPath()}/{mod.ID}/files/";
					
					Directory.CreateDirectory(filesPath);
					
					// Latest shas
					var shas = new Dictionary<string, Dir.File>();
					void walkDir(Dir dir) {
						foreach(var sub in dir.Dirs.Values)
							walkDir(sub);
						
						foreach(var file in dir.Files.Values)
							shas[file.Sha] = file;
					}
					walkDir(mod.Files.Dirs["files"]);
					shas[mod.Files.Files["config.json"].Sha] = mod.Files.Files["config.json"];
					
					// Get rid of outdated files
					foreach(var file in new DirectoryInfo(filesPath).EnumerateFiles()) {
						if(!shas.ContainsKey(file.Name)) {
							file.Delete();
						} else
							shas.Remove(file.Name);
					}
					
					// Download new files
					var counterLock = new System.Object();
					var activeCount = 0;
					
					async Task downloadFile(Dir.File file) {
						InstallStatus.CurrentJob = $"Downloading {mod.Name}";
						InstallStatus.CurrentJobDetails = $"({Regex.Match(file.Path, @"^.+/files/(.+)").Groups[1].Value})";
						
						lock(counterLock)
							activeCount++;
						
						Stream download;
						if(mod.Repo.Author == "")
							download = File.Open(file.Path, FileMode.Open);
						else
							download = await(await httpClient.GetAsync(file.Path, HttpCompletionOption.ResponseHeadersRead)).Content.ReadAsStreamAsync();
						
						var dataPtr = Marshal.AllocHGlobal(file.Size);
						
						try {
							unsafe {
								var data = (byte*)dataPtr;
								var i = 0;
								var buffer = new byte[65536];
								var readCount = 0;
								var reading = true;
								while(reading) {
									readCount = download.Read(buffer, 0, 65536);
									if(readCount == 0)
										break;
									
									for(int j = 0; j < readCount; j++) {
										if(i == file.Size) {
											reading = false;
											break; // just to be safe
										}
										
										data[i] = buffer[j];
										i++;
									}
								}
								
								ConvertToGameFile(file.Path.Split(".").Last(), filesPath + file.Sha, new(data, file.Size));
							}
						} catch {}
						
						Marshal.FreeHGlobal(dataPtr);
						download.Dispose();
						
						lock(counterLock)
							activeCount--;
						
						lock(InstallStatus)
							InstallStatus.Progress++;
					}
					
					InstallStatus.Progress = 0;
					InstallStatus.Total =  shas.Count;
					foreach(var a in shas) {
						while(activeCount >= 100)
							await Task.Delay(100);
						
						downloadFile(a.Value);
					}
					
					while(activeCount > 0)
						await Task.Delay(100);
					
					if(!mod.LoadConfig())
						mod.SaveConfig();
					
					Apply(mod);
					// TODO: tell penumbra to add/reload mod
					// TODO: make pr to penumbra to add those ipc
					
					InstallStatus.Busy = false;
					
					Aetherment.Interface.UiBuilder.AddNotification(mod.Name + " has been successfully installed and applied", "Mod Installed", Dalamud.Interface.Internal.Notifications.NotificationType.Success, 5000);
					
					lock(downloadQueue)
						downloadQueue.RemoveAt(0);
				}
			});
		}
		
		public static void Apply(Mod mod, bool onlyCustomize = false) {
			// TODO: this
			var penumPath = $"{Penumbra.GetModPath()}/{mod.ID}/";
			var filesPath = $"{penumPath}files/";
			
			InstallStatus.Busy = true;
			
			if(onlyCustomize) {
				var files = File.ReadAllText($"{filesPath}_").Split('\n');
				InstallStatus.Total = files.Length;
				foreach(var line in files) {
					var segs = line.Split('|');
					if(segs.Length < 2)
						break;
					
					var p = new Dictionary<string, string>();
					for(var i = 1; i < segs.Length; i++) {
						var f = segs[i].Split(':');
						p[f[0]] = filesPath + f[1];
					}
					
					InstallStatus.Progress++;
					InstallStatus.CurrentJob = $"Applying {mod.Name}";
					InstallStatus.CurrentJobDetails = $"({segs[0]})";
					ResolveCustomizability(p, mod.Options, filesPath + segs[0]);
				}
				
				InstallStatus.Busy = false;
				return;
			}
			
			var penumMeta = new PMeta{
				Name = mod.Name,
				Author = mod.Author,
				Description = mod.Description,
				Version = "todo"
			};
			
			var cus = File.CreateText($"{filesPath}_");
			
			// get the game path for each file and apply options
			var paths = new Dictionary<string, string>();
			void walkDir(Dir dir, string path, bool isfiledir) {
				InstallStatus.Total++;
				
				foreach(var sub in dir.Dirs.Values)
					walkDir(sub, path + (path == "" ? "" : "/") + sub.Name, isfiledir || sub.Name.Contains("."));
				
				if(dir.Files.Count > 0) {
					if(isfiledir) {
						var p = new Dictionary<string, string>();
						foreach(var file in dir.Files.Values)
							p[file.Name] = filesPath + file.Sha;
						
						InstallStatus.CurrentJob = $"Applying {mod.Name}";
						InstallStatus.CurrentJobDetails = $"({path})";
						if(ResolveCustomizability(p, mod.Options, $"{filesPath}_{dir.Sha}")) {
							paths[path] = "_" + dir.Sha;
							
							cus.Write($"_{dir.Sha}|");
							var i = 0;
							foreach(var file in dir.Files.Values) {
								cus.Write($"{file.Name}:{file.Sha}{(i < p.Count - 1 ? "|" : "\n")}");
								i++;
							}
						} else
							paths[path] = dir.Files.Values.First().Sha;
					} else
						foreach(var file in dir.Files.Values)
							paths[$"{path}/{file.Name}"] = file.Sha;
				}
				
				InstallStatus.Progress++;
			}
			walkDir(mod.Files.Dirs["files"], "", false);
			
			cus.Flush();
			cus.Dispose();
			
			// Get all files a specific option must have
			var penumOptionPaths = new Dictionary<string, List<string>>();
			foreach(var option in mod.Options) {
				if(option is Mod.Option.Penumbra o) {
					if(!penumOptionPaths.ContainsKey(o.Name))
						penumOptionPaths[o.Name] = new();
					
					foreach(var subOption in o.Options)
						foreach(var path in subOption.Value) {
							var gamePath = Regex.Match(path, @"^[^.]+\.[a-zA-Z]+").Groups[0].Value;
							
							if(!penumOptionPaths[o.Name].Contains(gamePath))
								penumOptionPaths[o.Name].Add(gamePath);
						}
				}	
			}
			
			// Create penumbra options
			foreach(var option in mod.Options) {
				if(option is Mod.Option.Penumbra o) {
					var typ = (o is Mod.Option.Single) ? "single" : "multi";
					if(!penumMeta.Groups.ContainsKey(o.Name))
						penumMeta.Groups[o.Name] = new PMeta.PGroup{
							GroupName = o.Name,
							SelectionType = typ
						};
					
					foreach(var subOption in o.Options) {
						var s = new PMeta.PGroup.POption{
							OptionName = subOption.Key
						};
						penumMeta.Groups[o.Name].Options.Add(s);
						
						var done = new List<string>();
						foreach(var path in subOption.Value) {
							// if(path == "ui/uld/icona_frame_hr1.tex/option/hide_macro_icon")
							// 	continue;
							
							var gamePath = Regex.Match(path, @"^[^.]+\.[a-zA-Z]+").Groups[0].Value;
							// s.OptionFiles["files\\" + paths[path]] = new string[1] {gamePath};
							s.OptionFiles["files\\" + paths[path]] = subOption.Value
								.Where(x => paths[x] == paths[path])
								.Select(x => Regex.Match(x, @"^[^.]+\.[a-zA-Z]+").Groups[0].Value)
								.ToArray();
							done.Add(gamePath);
						}
						
						if(typ == "single")
							foreach(var path in penumOptionPaths[o.Name])
								if(!done.Contains(path) && paths.ContainsKey(path))
									// s.OptionFiles["files\\" + paths[path]] = new string[1] {path};
									s.OptionFiles["files\\" + paths[path]] = penumOptionPaths[o.Name]
										.Where(x => paths[x] == paths[path])
										.ToArray();
					}
				}
			}
			
			// Create the "option" for non option files, super hacky but its amazing
			var defaults = new PMeta.PGroup.POption{
				OptionName = "_default"
			};
			penumMeta.Groups["_default"] = new PMeta.PGroup{
				GroupName = "_default",
				SelectionType = "single",
				Options = new List<PMeta.PGroup.POption> {defaults}
			};
			
			foreach(var path in paths) {
				var gamePath = Regex.Match(path.Key, @"^[^.]+\.[a-zA-Z]+").Groups[0].Value;
				// dont bother with unused option files
				if(gamePath != path.Key)
					continue;
				
				if(!penumOptionPaths.Values.Any(x => x.Any(y => y == gamePath)))
					// defaults.OptionFiles["files\\" + path.Value] = new string[1] {path.Key};
					defaults.OptionFiles["files\\" + path.Value] = paths.Keys
						.Where(x => paths[x] == path.Value && !penumOptionPaths.Values
							.Any(z => z
								.Any(y => y == Regex.Match(x, @"^[^.]+\.[a-zA-Z]+").Groups[0].Value)))
						.ToArray();
			}
			
			File.WriteAllText(penumPath + "meta.json", JsonConvert.SerializeObject(penumMeta, Formatting.Indented));
			
			// mod.SaveConfig();
			Aetherment.AddInstalledMod(mod.ID);
			
			InstallStatus.Busy = false;
		}
		
		// Probably turn the formats into an interface so i can just loop through or smth idk
		private static bool ResolveCustomizability(Dictionary<string, string> files, List<Mod.Option> options, string outpath) {
			var ext = files.Keys.First().Split('.').Last();
			if(ext == "dds" || ext == "png")
				return Tex.ResolveCustomizability(files, options, outpath);
			
			return false;
		}
		
		private static void ConvertToGameFile(string ext, string path, Buffer data) {
			if(ext == "dds")
				Tex.ConvertFromDDS(path, data);
			else if(ext == "png")
				Tex.ConvertFromPNG(path, data);
			else
				Raw.Write(path, data);
		}
		
		public static void Initialize() {
			InstallStatus = new();
			
			var handler = new HttpClientHandler();
			handler.Proxy = null;
			handler.UseProxy = false;
			
			httpClient = new HttpClient(handler);
			httpClient.DefaultRequestHeaders.Add("User-Agent", "FFXIV-Aetherment");
		}
		
		public static void Dispose() {
			httpClient.Dispose();
		}
		
		public static Task<string> GetString(string url) {
			if(url.Substring(0, 4) == "http")
				return httpClient.GetStringAsync(url);
			
			return File.ReadAllTextAsync(url);
		}
		
		public static Task<byte[]> GetByteArray(string url) {
			if(url.Substring(0, 4) == "http")
				return httpClient.GetByteArrayAsync(url);
			
			return File.ReadAllBytesAsync(url);
		}
		
		public static void DownloadMod(Mod mod) {
			downloadQueue = downloadQueue ?? new();
			
			if(downloadQueue.Contains(mod))
				return;
			
			lock(downloadQueue) {
				downloadQueue.Add(mod);
			
				if(downloadQueue.Count == 1)
					StartDownloader();
			}
		}
		
		public static void DeleteMod(Mod mod) {
			Task.Run(async() => {
				try {
					Directory.Delete(Path.GetFullPath($"{Penumbra.GetModPath()}/{mod.ID}"), true);
				} catch {}
				
				Aetherment.DeleteInstalledMod(mod.ID);
			});
		}
	}
}