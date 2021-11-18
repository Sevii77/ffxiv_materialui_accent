// TODO: Make it less of a mess

using Dalamud.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MaterialUI {
	public struct RepoFile {
		public string path;
		public string mode;
		public string type;
		public string sha;
		public string url;
	}
	
	public struct Repo {
		public string sha;
		public string url;
		public RepoFile[] tree;
	}
	
	public class Dir {
		public string name;
		public string sha;
		public Dictionary<string, string> files;
		public Dictionary<string, Dir> dirs;
		
		public Dir(string name, string sha) {
			this.name = name;
			this.sha = sha;
			files = new Dictionary<string, string>();
			dirs = new Dictionary<string, Dir>();
		}
	}
	
	public struct Color {
		public byte r;
		public byte g;
		public byte b;
	}
	
	public struct OptionColor {
		public string id;
		public string name;
		public Color @default;
	}
	
	public struct OptionPenumbra {
		public string name;
		public Dictionary<string, string[]> options;
	}
	
	public struct Options {
		[JsonProperty("color_options")]
		public OptionColor[] colorOptions;
		[JsonProperty("penumbra")]
		public OptionPenumbra[] penumbraOptions;
	}
	
	public class MetaGroupOption {
		public string OptionName;
		public string OptionDesc;
		public Dictionary<string, string[]> OptionFiles;
		
		public MetaGroupOption(string name) {
			OptionName = name;
			OptionDesc = "";
			OptionFiles = new Dictionary<string, string[]>();
		}
	}
	
	public class MetaGroup {
		public string GroupName;
		public string SelectionType;
		public List<MetaGroupOption> Options;
		
		public MetaGroup(string name) {
			GroupName = name;
			SelectionType = "single";
			Options = new List<MetaGroupOption>();
		}
	}
	
	public class Meta {
		public int FileVersion;
		public string Name;
		public string Author;
		public string Description;
		public string Version;
		public string Website;
		public Dictionary<string, string> FileSwaps;
		public Dictionary<string, MetaGroup> Groups;
		
		public Meta() {
			FileVersion = 0;
			Name = "Material UI";
			Author = "Sevii, skotlex";
			Description = "";
			Version = "Latest, probably";
			Website = "https://github.com/Sevii77/ffxiv_materialui_accent";
			FileSwaps = new Dictionary<string, string>();
			Groups = new Dictionary<string, MetaGroup>();
		}
	}
	
	public class Updater {
		private const string repoMaster = "skotlex/ffxiv-material-ui";
		private const string repoAccent = "sevii77/ffxiv_materialui_accent";
		
		private HttpClient httpClient;
		private MaterialUI main;
		
		public Options options {get; private set;}
		public Dir dirMaster {get; private set;}
		public Dir dirAccent {get; private set;}
		
		public bool downloading {get; private set;} = false;
		public string statusText {get; private set;} = "";
		
		public Updater(MaterialUI main) {
			this.main = main;
			
			var handler = new HttpClientHandler();
			handler.Proxy = null;
			handler.UseProxy = false;
			
			httpClient = new HttpClient(handler);
			httpClient.DefaultRequestHeaders.Add("User-Agent", "FFXIV-MaterialUI-Accent");
		}
		
		private Dir PopulateDir(Repo repo, string repoName) {
			Dir dir = new Dir("", repo.sha);
			
			foreach(RepoFile file in repo.tree) {
				Dir curdir = dir;
				string[] path = file.path.Split("/");
				
				if(file.type == "tree") {
					for(int i = 0; i < path.Length - 1; i++)
						curdir = curdir.dirs[path[i]];
					
					string n = path[path.Length - 1];
					curdir.dirs[n] = new Dir(n, file.sha);
				} if(file.type == "blob") {
					for(int i = 0; i < path.Length - 1; i++)
						curdir = curdir.dirs[path[i]];
					
					curdir.files[path[path.Length - 1]] = String.Format("https://raw.githubusercontent.com/{0}/master/{1}", repoName, file.path);
				}
			}
			
			return dir;
		}
		
		private async Task<List<string>> UpdateCache(Dir[] dirs) {
			// Get all latest shas
			List<string> texturesLatest = new List<string>();
			Dictionary<string, Dir> shaLatest = new Dictionary<string, Dir>();
			foreach(Dir dir in dirs)
				foreach(KeyValuePair<string, Dir> d in dir.dirs) {
					string name = d.Key.ToLower();
					if(!texturesLatest.Contains(name)) {
						texturesLatest.Add(name);
						shaLatest[d.Value.sha] = d.Value;
					}
				}
			
			// Get rid of caches textures that are outdated
			List<string> shaCurrent = new List<string>();
			foreach(var dir in main.pluginInterface.ConfigDirectory.GetDirectories()) {
				string sha = dir.Name;
				if(shaLatest.ContainsKey(sha))
					shaCurrent.Add(sha);
				else
					Directory.Delete(Path.GetFullPath(main.pluginInterface.ConfigDirectory + "/" + sha), true);
			}
			
			// Download and cache new shas
			List<(Dir, string)> queue = new List<(Dir, string)>();
			void addToQueue(Dir dir, string path) {
				if(dir.files.Count > 0)
					queue.Add((dir, path));
				
				foreach(KeyValuePair<string, Dir> dir2 in dir.dirs)
					addToQueue(dir2.Value, path + dir2.Key + "/");
			}
			
			List<string> changes = new List<string>();
			foreach(KeyValuePair<string, Dir> sha in shaLatest)
				if(!shaCurrent.Contains(sha.Key)) {
					changes.Add(sha.Value.name);
					addToQueue(sha.Value, main.pluginInterface.ConfigDirectory + "/" + sha.Key + "/");
				}
			int total = queue.Count;
			
			async void download(Dir dir, string path) {
				// statusText = "Downloading\n" + dir.name;
				statusText = string.Format("Downloading ({0}/{1})\n{2}", total - queue.Count, total, dir.name);
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				
				foreach(KeyValuePair<string, string> file in dir.files)
					if(file.Key.Contains(".dds"))
						File.WriteAllBytes(Path.GetFullPath(path + file.Key), await httpClient.GetByteArrayAsync(file.Value));
			}
			
			async Task downloader() {
				downloading = true;
				
				while(queue.Count > 0) {
					for(int i = 0; i < Math.Min(queue.Count, 50); i++) {
						download(queue[0].Item1, queue[0].Item2);
						queue.RemoveAt(0);
					}
					
					await Task.Delay(1000);
				}
				
				downloading = false;
			}
			
			await downloader();
			
			return changes;
		}
		
		public void LoadOptions() {
			Task.Run(async() => {
				#if RELEASE
				string resp = await httpClient.GetStringAsync(String.Format("https://raw.githubusercontent.com/{0}/master/options.json", repoAccent));
				#elif DEBUG
				string resp = await httpClient.GetStringAsync("https://sevii.dev/materialui/options.json");
				PluginLog.LogDebug("debug options");
				#endif
				
				resp = Regex.Replace(resp, "//[^\n]*", "");
				options = JsonConvert.DeserializeObject<Options>(resp);
				
				main.ui.colorOptions = new Vector3[options.colorOptions.Length];
				
				for(int i = 0; i < options.colorOptions.Length; i++) {
					OptionColor option = options.colorOptions[i];
					Vector3 clr = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
					
					if(!main.config.colorOptions.ContainsKey(option.id))
						main.config.colorOptions[option.id] = clr;
					
					main.ui.colorOptions[i] = main.config.colorOptions[option.id];
				}
			});
		}
		
		public void Update() {
			Task.Run(async() => {
				string respMaster = await httpClient.GetStringAsync(String.Format("https://api.github.com/repos/{0}/git/trees/master?recursive=1", repoMaster));
				Repo dataMaster = JsonConvert.DeserializeObject<Repo>(respMaster);
				dirMaster = PopulateDir(dataMaster, repoMaster);
				
				string respAccent = await httpClient.GetStringAsync(String.Format("https://api.github.com/repos/{0}/git/trees/master?recursive=1", repoAccent));
				Repo dataAccent = JsonConvert.DeserializeObject<Repo>(respAccent);
				dirAccent = PopulateDir(dataAccent, repoAccent);
				
				if(main.config.openOnStart)
					main.ui.settingsVisible = true;
				
				List<string> changes = await UpdateCache(new Dir[3] {
					dirAccent.dirs["elements_" + main.config.style].dirs["ui"].dirs["uld"],
					dirMaster.dirs["4K resolution"].dirs[char.ToUpper(main.config.style[0]) + main.config.style.Substring(1)].dirs["Saved"].dirs["UI"].dirs["HUD"],
					dirMaster.dirs["4K resolution"].dirs[char.ToUpper(main.config.style[0]) + main.config.style.Substring(1)].dirs["Saved"].dirs["UI"].dirs["Icon"].dirs["Icon"]
				});
				
				if(main.config.firstTime)
					return;
				
				List<string> optionsNew = new List<string>();
				string penumbraConfigPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/XIVLauncher/PluginConfigs/Penumbra.json");
				if(File.Exists(penumbraConfigPath)) {
					dynamic penumbraData = JsonConvert.DeserializeObject(File.ReadAllText(penumbraConfigPath));
					string penumbraPath = (string)penumbraData?.ModDirectory;
					if(penumbraPath != "") {
						string metaPath = Path.GetFullPath(penumbraPath + "/Material UI/meta.json");
						if(File.Exists(metaPath)) {
							List<(string, string)> optionsCurrent = new List<(string, string)>();
							
							Meta meta = JsonConvert.DeserializeObject<Meta>(File.ReadAllText(metaPath));
							foreach(MetaGroup group in meta.Groups.Values)
								foreach(MetaGroupOption option in group.Options)
									optionsCurrent.Add((group.GroupName, option.OptionName));
							
							foreach(OptionPenumbra group in options.penumbraOptions) {
								List<string> optionStr = new List<string>();
								foreach(string option in group.options.Keys)
									if(!optionsCurrent.Contains((group.name, option)))
										optionStr.Add(" " + option);
								
								if(optionStr.Count > 0)
									optionsNew.Add(group.name + "\n" + string.Join("\n", optionStr));
							}
						}
					}
				}
				
				if(changes.Count == 0 && optionsNew.Count == 0)
					return;
				
				if(optionsNew.Count > 0)
					main.ui.ShowNotice(string.Format("Material UI has been updated\nPlease rediscover mods in Penumbra\n\nNew Options:\n{0}\n\nUpdated Files:\n{1}", string.Join("\n\n", optionsNew), string.Join("\n", changes)));
				else
					main.ui.ShowNotice(string.Format("Material UI has been updated\nPlease rediscover mods in Penumbra\n\nUpdated Files:\n{0}", string.Join("\n", changes)));
				
				Apply();
			});
		}
		
		// TODO: use penumbra api once its ready
		public void Apply() {
			string penumbraConfigPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/XIVLauncher/PluginConfigs/Penumbra.json");
			if(!File.Exists(penumbraConfigPath)) {
				main.ui.ShowNotice("Can't find Penumbra Config,\nis it even installed?");
				
				return;
			}
			
			dynamic penumbraData = JsonConvert.DeserializeObject(File.ReadAllText(penumbraConfigPath));
			if(!(bool)penumbraData?.IsEnabled) {
				main.ui.ShowNotice("Penumbra is disabled.");
				
				return;
			}
			
			string penumbraPath = (string)penumbraData?.ModDirectory;
			if(penumbraPath == "") {
				main.ui.ShowNotice("Penumbra Mod Directory has not been set.");
				
				return;
			}
			
			try {
				Directory.Delete(Path.GetFullPath(penumbraPath + "/Material UI"), true);
			} catch(Exception e) {}
			
			// Used to check if an option exists, avoids cases where an option is used and the default ignored, thus creating the texture 2 times
			List<string> optionPaths = new List<string>();
			foreach(OptionPenumbra option in options.penumbraOptions)
				foreach(KeyValuePair<string, string[]> subOptions in option.options)
					foreach(string path in subOptions.Value) {
						optionPaths.Add(path);
						optionPaths.Add(path.Split("/option/")[0].Split("/OPTIONS/")[0]);
					}
			
			Meta meta = new Meta();
			meta.Description = "Open the configurator with /materialui\n\nCurrent colors:";
			
			Vector3 clr = main.config.color;
			meta.Description += string.Format("\nAccent: R:{0} G:{1} B:{2}", (byte)(clr.X * 255), (byte)(clr.Y * 255), (byte)(clr.Z * 255));
			
			foreach(OptionColor optionColor in options.colorOptions) {
				clr = main.config.colorOptions[optionColor.id];
				meta.Description += string.Format("\n{0}: R:{1} G:{2} B:{3}", optionColor.name, (byte)(clr.X * 255), (byte)(clr.Y * 255), (byte)(clr.Z * 255));
			}
			
			// Create options now so its in the correct order
			foreach(OptionPenumbra option in options.penumbraOptions) {
				meta.Groups[option.name] = new MetaGroup(option.name);
				
				foreach(KeyValuePair<string, string[]> subOptions in option.options)
					meta.Groups[option.name].Options.Add(new MetaGroupOption(subOptions.Key));
			}
			
			void writeTex(Tex tex, string texturePath, string gamePath) {
				if(optionPaths.Contains(texturePath)) {
					foreach(OptionPenumbra option in options.penumbraOptions)
						foreach(KeyValuePair<string, string[]> subOptions in option.options)
							foreach(string p in subOptions.Value)
								if(p == texturePath) {
									string optionName = option.name;
									string subOptionName = subOptions.Key;
									
									int index = -1;
									for(int i = 0; i < meta.Groups[optionName].Options.Count; i++)
										if(meta.Groups[optionName].Options[i].OptionName == subOptionName) {
											index = i;
											break;
										}
									
									string path = Path.GetFullPath(penumbraPath + "/Material UI/" + optionName + "/" + subOptionName + "/" + gamePath);
									
									meta.Groups[optionName].Options[index].OptionFiles[(optionName + "/" + subOptionName + "/" + gamePath + "_hr1.tex").Replace("/", "\\")] = new string[1] {gamePath + "_hr1.tex"};
									Directory.CreateDirectory(Path.GetDirectoryName(path));
									tex.Save(path + "_hr1.tex");
								}
				} else {
					string path = Path.GetFullPath(penumbraPath + "/Material UI/" + gamePath);
					Directory.CreateDirectory(Path.GetDirectoryName(path));
					tex.Save(path + "_hr1.tex");
				}
			}
			
			List<string> shas = new List<string>();
			foreach(var dir in main.pluginInterface.ConfigDirectory.GetDirectories())
				shas.Add(dir.Name);
			
			void walkDirAccent(Dir dir, string fullPath, string cachePath) {
				if(shas.Contains(dir.sha))
					cachePath = "/" + dir.sha + "/";
				else if(cachePath != null)
					cachePath += dir.name + "/";
				
				if(dir.files.Count > 0 && cachePath != null) {
					string path = main.pluginInterface.ConfigDirectory + cachePath;
					
					Tex tex = new Tex(File.ReadAllBytes(Path.GetFullPath(path + "underlay.dds")));
					Tex overlay = new Tex(File.ReadAllBytes(Path.GetFullPath(path + "overlay.dds")));
					overlay.Paint(main.config.color);
					tex.Overlay(overlay);
					
					foreach(OptionColor optionColor in options.colorOptions) {
						string overlayColorName = string.Format("overlay_{0}.dds", optionColor.id);
						if(dir.files.ContainsKey(overlayColorName)) {
							Tex overlayColor = new Tex(File.ReadAllBytes(Path.GetFullPath(path + overlayColorName)));
							overlayColor.Paint(main.config.colorOptions[optionColor.id]);
							tex.Overlay(overlayColor);
						}
					}
					
					string gamePath = fullPath.Split("/option/")[0];
					writeTex(tex, fullPath, gamePath);
				}
				
				foreach(KeyValuePair<string, Dir> d in dir.dirs)
					walkDirAccent(d.Value, fullPath == null ? d.Key : (fullPath + "/" + d.Key), cachePath);
			}
			
			walkDirAccent(dirAccent.dirs["elements_" + main.config.style], null, null);
			
			if(!main.config.accentOnly) {
				void walkDirMain(Dir dir, string fullPath, string cachePath) {
					if(shas.Contains(dir.sha))
						cachePath = "/" + dir.sha + "/";
					else if(cachePath != null)
						cachePath += dir.name + "/";
					
					if(dir.files.Count > 0 && cachePath != null) {
						foreach(string filename in dir.files.Keys) {
							if(!filename.Contains(".dds"))
								continue;
							
							string path = main.pluginInterface.ConfigDirectory + cachePath;
							Tex tex = new Tex(File.ReadAllBytes(Path.GetFullPath(path + filename)));
							
							string gamePath = fullPath.Split("/OPTIONS/")[0].ToLower().Replace("/hud/", "/uld/");
							if(gamePath.Contains("/icon/icon/"))
								gamePath = gamePath.Replace("/icon/icon/", Regex.Match(gamePath, @"(/icon/\d\d\d)").Value + "000/");
							writeTex(tex, fullPath, gamePath);
						}
					}
					
					foreach(KeyValuePair<string, Dir> d in dir.dirs)
						walkDirMain(d.Value, fullPath == null ? d.Key : (fullPath + "/" + d.Key), cachePath);
				}
				
				walkDirMain(dirMaster.dirs["4K resolution"].dirs[char.ToUpper(main.config.style[0]) + main.config.style.Substring(1)].dirs["Saved"], null, null);
			} else {
				// Get rid of unused options
				foreach(KeyValuePair<string, MetaGroup> group in meta.Groups) {
					List<MetaGroupOption> options2 = new List<MetaGroupOption>();
					foreach(MetaGroupOption option in group.Value.Options) {
						if(option.OptionFiles.Count > 0) {
							options2.Add(option);
						}
					}
					
					group.Value.Options = options2;
				}
			}
			
			File.WriteAllText(Path.GetFullPath(penumbraPath + "/Material UI/meta.json"), JsonConvert.SerializeObject(meta, Formatting.Indented));
		}
	}
}