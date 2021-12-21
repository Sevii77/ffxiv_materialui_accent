// TODO: Make it less of a mess
// TODO: Generate Default suboption if the option is from a mod

using Dalamud.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ImGuiScene;

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
		public Dictionary<string, (string, string)> files;
		public Dictionary<string, Dir> dirs;
		
		public Dir(string name, string sha) {
			this.name = name;
			this.sha = sha;
			files = new Dictionary<string, (string, string)>();
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
		// Mod properties
		public string author;
		[JsonProperty("author_contact")]
		public string authorContact;
		public string name;
		public string description;
		
		// Generic
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
			Version = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
			Website = "https://github.com/Sevii77/ffxiv_materialui_accent";
			FileSwaps = new Dictionary<string, string>();
			Groups = new Dictionary<string, MetaGroup>();
		}
	}
	
	public class Mod {
		public string id;
		public string repo;
		public Options options;
		public Dir dir;
		public TextureWrap preview;
		
		public Mod(string id, string repo, Options options, Dir dir, TextureWrap preview) {
			this.id = id;
			this.repo = repo;
			this.options = options;
			this.dir = dir;
			this.preview = preview;
		}
	}
	
	public class Updater {
		public const string repoMaster = "skotlex/ffxiv-material-ui";
		public const string repoAccent = "sevii77/ffxiv_materialui_accent";
		
		private HttpClient httpClient;
		private MaterialUI main;
		
		// public Options options {get; private set;}
		public Dir dirMaster {get; private set;}
		public Dir dirAccent {get; private set;}
		public Dictionary<string, Dir> dirMods {get; private set;}
		public Dictionary<string, Mod> mods {get; private set;}
		
		public bool busy {get; private set;} = false;
		public string statusText {get; private set;} = "";
		
		public Updater(MaterialUI main) {
			this.main = main;
			
			dirMods = new Dictionary<string, Dir>();
			mods = new Dictionary<string, Mod>();
			
			var handler = new HttpClientHandler();
			handler.Proxy = null;
			handler.UseProxy = false;
			
			httpClient = new HttpClient(handler);
			httpClient.DefaultRequestHeaders.Add("User-Agent", "FFXIV-MaterialUI-Accent");
		}
		
		private async Task<string> GetStringAsync(string path) {
			if(path.Substring(0, 4) == "http")
				return await httpClient.GetStringAsync(path);
			
			return File.ReadAllText(path);
		}
		
		private async Task<byte[]> GetBytesAsync(string path) {
			if(path.Substring(0, 4) == "http")
				return await httpClient.GetByteArrayAsync(path);
			
			return File.ReadAllBytes(path);
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
					
					curdir.files[path[path.Length - 1]] = (file.sha, String.Format("https://raw.githubusercontent.com/{0}/master/{1}", repoName, file.path));
				}
			}
			
			return dir;
		}
		
		private async Task<List<string>> UpdateCache(List<Dir> dirs) {
			// Get all latest shas
			Dictionary<string, (string, string)> shaLatest = new Dictionary<string, (string, string)>();
			void walkDir(Dir dir, string path) {
				foreach(KeyValuePair<string, (string, string)> file in dir.files)
					if(file.Key.Contains(".dds"))
						shaLatest[file.Value.Item1] = (file.Value.Item2, path + "/" + file.Key);
				
				foreach(Dir subdir in dir.dirs.Values)
					walkDir(subdir, path + "/" + subdir.name);
			}
			
			foreach(Dir dir in dirs)
				if(dir != null)
					walkDir(dir, "");
			
			// Get rid old cache system files
			foreach(var dir in main.pluginInterface.ConfigDirectory.GetDirectories()) {
				Directory.Delete(dir.FullName, true);
			}
			
			// Get rid of caches textures that are outdated
			List<string> shaCurrent = new List<string>();
			foreach(var file in main.pluginInterface.ConfigDirectory.GetFiles()) {
				string sha = file.Name;
				if(shaLatest.ContainsKey(sha))
					shaCurrent.Add(sha);
				else
					file.Delete();
			}
			//
			// Download and cache new shas
			List<string> changes = new List<string>();
			List<(string, string, string)> queue = new List<(string, string, string)>();
			foreach(KeyValuePair<string, (string, string)> sha in shaLatest)
				if(!shaCurrent.Contains(sha.Key)) {
					queue.Add((sha.Value.Item1, sha.Value.Item2, sha.Key));
					changes.Add(sha.Value.Item2);
				}
			int total = queue.Count;
			
			int busycount = 0;
			async Task download(string url, string name, string sha) {
				statusText = string.Format("Downloading ({0}/{1})\n{2}", total - queue.Count, total, name);
				busycount++;
				
				try {
					using(Stream download = await (await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)).Content.ReadAsStreamAsync())
						using(Stream write = File.Open(Path.GetFullPath(main.pluginInterface.ConfigDirectory + "/" + sha), FileMode.Create))
							await download.CopyToAsync(write);
				} catch(Exception e) {
					// It failed, just add it back to the queue
					// queue.Add((dir, path));
				}
				
				busycount--;
			}
			
			async Task downloader() {
				busy = true;
				while(queue.Count > 0) {
					for(int i = 0; i < Math.Min(queue.Count, 50); i++) {
						download(queue[0].Item1, queue[0].Item2, queue[0].Item3);
						queue.RemoveAt(0);
					}
					
					while(busycount > 0) {
						await Task.Delay(1000);
					}
				}
				busy = false;
			}
			
			await downloader();
			
			return changes;
		}
		
		public async Task<List<string>> UpdateCache() {
			List<Dir> toDownload = new List<Dir>();
			
			// Add paths from accent
			toDownload.Add(dirAccent.GetPathDir(string.Format("elements_{0}", main.config.style)));
			// toDownload.Add(dirAccent.GetPathDir(string.Format("elements_{0}/ui/uld", main.config.style)));
			// toDownload.Add(dirAccent.GetPathDir(string.Format("elements_{0}/ui/icon", main.config.style)));
			
			// Add paths from main
			string mainStyleName = char.ToUpper(main.config.style[0]) + main.config.style.Substring(1);
			toDownload.Add(dirMaster.GetPathDir(string.Format("4K resolution/{0}/Saved", mainStyleName)));
			// toDownload.Add(dirMaster.GetPathDir(string.Format("4K resolution/{0}/Saved/UI/HUD", mainStyleName)));
			// toDownload.Add(dirMaster.GetPathDir(string.Format("4K resolution/{0}/Saved/UI/Icon/Icon", mainStyleName)));
			
			// Add paths from mods
			foreach(Mod mod in mods.Values)
				if(mod.id != "base" && mod.repo != "local" && main.config.modOptions[mod.id].enabled) {
					toDownload.Add(mod.dir.GetPathDir(string.Format("elements_{0}", main.config.style)));
					// toDownload.Add(mod.dir.GetPathDir(string.Format("elements_{0}/ui/uld", main.config.style)));
					// toDownload.Add(mod.dir.GetPathDir(string.Format("elements_{0}/ui/icon", main.config.style)));
				}
			
			return await UpdateCache(toDownload);
		}
		
		public async Task LoadOptions() {
			// Task.Run(async() => {
				string resp = Regex.Replace(await httpClient.GetStringAsync(String.Format("https://raw.githubusercontent.com/{0}/master/options.json", repoAccent)), "//[^\n]*", "");
				Options options = JsonConvert.DeserializeObject<Options>(resp);
				mods["base"] = new Mod(
					"base",
					"base",
					options,
					null,
					null
				);
				
				// TODO: use this instead
				// foreach(OptionColor option in options.colorOptions)
				// 	if(!main.config.colorOptions.ContainsKey(option.id))
				// 		main.config.colorOptions[option.id] = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
				
				main.ui.colorOptions = new Vector3[options.colorOptions.Length];
				
				for(int i = 0; i < options.colorOptions.Length; i++) {
					OptionColor option = options.colorOptions[i];
					Vector3 clr = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
					
					if(!main.config.colorOptions.ContainsKey(option.id))
						main.config.colorOptions[option.id] = clr;
					
					main.ui.colorOptions[i] = main.config.colorOptions[option.id];
				}
			// });
		}
		
		public async Task LoadMods() {
			dirMods.Clear();
			mods.Clear();
			
			await LoadOptions();
			mods["base"].dir = dirAccent;
			dirMods["Material UI"] = dirAccent.GetPathDir("mods");
			
			string resp;
			Repo data;
			
			// 3rd party
			foreach(string thirdparty in main.config.thirdPartyModRepos) {
				try {
					resp = await httpClient.GetStringAsync(String.Format("https://api.github.com/repos/{0}/git/trees/master?recursive=1", thirdparty));
					data = JsonConvert.DeserializeObject<Repo>(resp);
					dirMods[thirdparty] = PopulateDir(data, thirdparty).GetPathDir("mods");
				} catch(Exception e) {
					PluginLog.LogError(e, "Failed loading third party mod repository " + thirdparty);
				}
			}
			
			// Local
			if(main.config.localEnabled) {
				void walk(DirectoryInfo folder, Dir dir) {
					foreach(var file in folder.GetFiles())
						dir.files[file.Name] = (null, file.FullName);
					
					foreach(var sub in folder.GetDirectories()) {
						dir.dirs[sub.Name] = new Dir(sub.Name, "");
						walk(sub, dir.dirs[sub.Name]);
					}
				}
				
				DirectoryInfo local = new DirectoryInfo(main.config.localPath);
				if(local.Exists) {
					Dir dir = new Dir("", "");
					walk(local, dir);
					dirMods["local"] = dir;
				}
			}
			
			// Create mod structure
			foreach(KeyValuePair<string, Dir> modRepo in dirMods)
				foreach(KeyValuePair<string, Dir> mod in modRepo.Value.dirs) {
					PluginLog.Log(mod.Key);
					try {
						resp = Regex.Replace(await GetStringAsync(mod.Value.files["options.json"].Item2), "//[^\n]*", "");
						Options options = JsonConvert.DeserializeObject<Options>(resp);
						if(!mods.ContainsKey(mod.Key))
							mods[mod.Key] = new Mod(
								mod.Key,
								modRepo.Key,
								options,
								mod.Value,
								mod.Value.files.ContainsKey("preview.png") ? main.pluginInterface.UiBuilder.LoadImage(await GetBytesAsync(mod.Value.files["preview.png"].Item2)) : null
							);
						
						if(!main.config.modOptions.ContainsKey(mod.Key))
							main.config.modOptions[mod.Key] = new ModConfig();
						
						foreach(OptionColor option in options.colorOptions)
							if(!main.config.modOptions[mod.Key].colors.ContainsKey(option.id))
								main.config.modOptions[mod.Key].colors[option.id] = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
					} catch(Exception e) {
						PluginLog.LogError(e, "Failed prepairing mod repository " + mod.Key);
					}
				}
		}
		
		public void Update() {
			Task.Run(async() => {
				if(main.config.openOnStart)
					main.ui.settingsVisible = true;
				
				for(int i = 0; i < 5; i++) {
					try {
						main.pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion").InvokeFunc();
						
						break;
					} catch(Exception e) {
						if(i == 4) {
							PluginLog.Log("Penumbra not found, exiting updater");
							
							return;
						}
					}
					
					await Task.Delay(1000);
				}
				
				string resp = await httpClient.GetStringAsync(String.Format("https://api.github.com/repos/{0}/git/trees/master?recursive=1", repoMaster));
				Repo data = JsonConvert.DeserializeObject<Repo>(resp);
				dirMaster = PopulateDir(data, repoMaster);
				
				resp = await httpClient.GetStringAsync(String.Format("https://api.github.com/repos/{0}/git/trees/master?recursive=1", repoAccent));
				data = JsonConvert.DeserializeObject<Repo>(resp);
				dirAccent = PopulateDir(data, repoAccent);
				
				await LoadMods();
				List<string> changes = await UpdateCache();
				
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
							
							foreach(OptionPenumbra group in mods["base"].options.penumbraOptions) {
								List<string> optionStr = new List<string>();
								foreach(string option in group.options.Keys)
									if(!optionsCurrent.Contains((group.name, option)))
										optionStr.Add("\t" + option);
								
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
			try {
				main.pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion").InvokeFunc();
			} catch(Exception e) {
				return;
			}
			
			string penumbraConfigPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/XIVLauncher/PluginConfigs/Penumbra.json");
			if(!File.Exists(penumbraConfigPath)) {
				main.ui.ShowNotice("Can't find Penumbra Config.");
				
				return;
			}
			
			dynamic penumbraData = JsonConvert.DeserializeObject(File.ReadAllText(penumbraConfigPath));
			string penumbraPath = (string)penumbraData?.ModDirectory;
			if(penumbraPath == "") {
				main.ui.ShowNotice("Penumbra Mod Directory has not been set.");
				
				return;
			}
			
			Task.Run(async() => {
				busy = true;
				
				try {
					Directory.Delete(Path.GetFullPath(penumbraPath + "/Material UI"), true);
				} catch(Exception e) {}
				
				// Used to check if an option exists, avoids cases where an option is used and the default ignored, thus creating the texture 2 times
				List<string> optionPaths = new List<string>();
				foreach(Mod mod in mods.Values)
					if(mod.id == "base" || main.config.modOptions[mod.id].enabled)
						foreach(OptionPenumbra option in mod.options.penumbraOptions)
							foreach(KeyValuePair<string, string[]> subOptions in option.options)
								foreach(string path in subOptions.Value) {
									string gamePath = path.Split("/OPTIONS/")[0].Split("/option/")[0].ToLower().Replace("/hud/", "/uld/").Replace("/icon/icon/", "/icon/");
									if(gamePath.Contains("/icon/"))
										gamePath = gamePath.Replace("/icon/", Regex.Match(gamePath, @"(/icon/\d\d\d)").Value + "000/");
									optionPaths.Add(gamePath);
								}
				
				Meta meta = new Meta();
				meta.Description = "Open the configurator with /materialui\n\nCurrent colors:";
				
				Vector3 clr = main.config.color;
				meta.Description += string.Format("\nAccent: R:{0} G:{1} B:{2}", (byte)(clr.X * 255), (byte)(clr.Y * 255), (byte)(clr.Z * 255));
				
				foreach(Mod mod in mods.Values)
					if(mod.id == "base" || main.config.modOptions[mod.id].enabled)
						foreach(OptionColor optionColor in mod.options.colorOptions) {
							if(mod.id == "base")
								clr = main.config.colorOptions[optionColor.id];
							else
								clr = main.config.modOptions[mod.id].colors[optionColor.id];
							
							meta.Description += string.Format("\n{0}: R:{1} G:{2} B:{3}", optionColor.name, (byte)(clr.X * 255), (byte)(clr.Y * 255), (byte)(clr.Z * 255));
						}
				
				// Create options now so its in the correct order
				void createOptions(Mod mod) {
					foreach(OptionPenumbra option in mod.options.penumbraOptions) {
						if(!meta.Groups.ContainsKey(option.name)) {
							meta.Groups[option.name] = new MetaGroup(option.name);
							
							if(mod.id != "base") {
								meta.Groups[option.name].Options.Add(new MetaGroupOption("Default"));
								foreach(string[] textures in option.options.Values) {
									option.options["Default"] = textures;
									
									break;
								}
							}
						}
						
						foreach(string subOption in option.options.Keys)
							if(mod.id == "base" || subOption != "Default")
								meta.Groups[option.name].Options.Add(new MetaGroupOption(subOption));
					}
				}
				
				createOptions(mods["base"]);
				foreach(Mod mod in mods.Values)
					if(mod.id != "base" && main.config.modOptions[mod.id].enabled)
						createOptions(mod);
				
				void writeTex(Tex tex, string texturePath, string gamePath, string modid) {
					statusText = string.Format("Applying\n{0}/{1}", modid, gamePath);
					
					// Used to allow game style format for the options in main
					string texturePath2 = texturePath.ToLower().Replace("/options/", "/option/").Replace("/hud/", "/uld/").Replace("/icon/icon/", "/icon/");
					
					if(optionPaths.Contains(gamePath)) {
						List<Mod> priority = new List<Mod>();
						if(modid == "base" || modid == "main") {
							priority.Add(mods["base"]);
							
							foreach(Mod mod in mods.Values)
								if(mod.id != "base" && main.config.modOptions[mod.id].enabled)
									priority.Add(mod);
						} else {
							priority.Add(mods[modid]);
						}
						
						foreach(Mod mod in priority)
							foreach(OptionPenumbra option in mod.options.penumbraOptions)
								foreach(KeyValuePair<string, string[]> subOptions in option.options)
									foreach(string p in subOptions.Value)
										if(p == texturePath || p == texturePath2) {
											string optionName = option.name;
											string subOptionName = subOptions.Key;
											
											int index = -1;
											for(int i = 0; i < meta.Groups[optionName].Options.Count; i++)
												if(meta.Groups[optionName].Options[i].OptionName == subOptionName) {
													index = i;
													break;
												}
											
											string path = Path.GetFullPath(penumbraPath + "/Material UI/" + optionName + "/" + subOptionName + "/" + gamePath + "_hr1.tex");
											if(File.Exists(path))
												return;
											
											meta.Groups[optionName].Options[index].OptionFiles[(optionName + "/" + subOptionName + "/" + gamePath + "_hr1.tex").Replace("/", "\\")] = new string[1] {gamePath + "_hr1.tex"};
											
											Directory.CreateDirectory(Path.GetDirectoryName(path));
											tex.Save(path);
										}
					} else {
						string path = Path.GetFullPath(penumbraPath + "/Material UI/" + gamePath + "_hr1.tex");
						if(File.Exists(path))
							return;
						
						Directory.CreateDirectory(Path.GetDirectoryName(path));
						tex.Save(path);
					}
				}
				
				byte[] readFile(string path) {
					try {
						return File.ReadAllBytes(path);
					} catch(Exception e) {
						statusText = "Failed reading file\n\n" + e;
					}
					
					return null;
				}
				
				List<string> shas = new List<string>();
				foreach(var dir in main.pluginInterface.ConfigDirectory.GetDirectories())
					shas.Add(dir.Name);
				
				string cachepath = main.pluginInterface.ConfigDirectory + "/";
				void walkDirAccent(Dir dir, string fullPath, string modid) {
					if(dir.files.Count > 0) {
						Tex tex = null;
						if(dir.files.ContainsKey("underlay.dds")) {
							var file = dir.files["underlay.dds"];
							
							if(file.Item2 != null)
								tex = new Tex(readFile(Path.GetFullPath(cachepath + file.Item1)));
							else
								tex = new Tex(readFile(file.Item2));
						}
								
						
						if(dir.files.ContainsKey("overlay.dds")) {
							(string, string) file = dir.files["overlay.dds"];
							
							Tex overlay;
							if(file.Item2 != null)
								overlay = new Tex(readFile(Path.GetFullPath(cachepath + file.Item1)));
							else
								overlay = new Tex(readFile(file.Item2));
							
							overlay.Paint(main.config.color);
							
							if(tex != null)
								tex.Overlay(overlay);
							else
								tex = overlay;
						}
						
						foreach(Mod mod in mods.Values)
							if(mod.id == modid || mod.id == "base")
								foreach(OptionColor optionColor in mod.options.colorOptions) {
									string overlayColorName = string.Format("overlay_{0}.dds", optionColor.id);
									if(dir.files.ContainsKey(overlayColorName)) {
										(string, string) file = dir.files[overlayColorName];
										
										Tex overlayColor;
										if(file.Item2 != null)
											overlayColor = new Tex(readFile(Path.GetFullPath(cachepath + file.Item1)));
										else
											overlayColor = new Tex(readFile(file.Item2));
										
										if(mod.id == "base")
											overlayColor.Paint(main.config.colorOptions[optionColor.id]);
										else
											overlayColor.Paint(main.config.modOptions[mod.id].colors[optionColor.id]);
										
										if(tex != null)
											tex.Overlay(overlayColor);
										else
											tex = overlayColor;
									}
								}
						
						string gamePath = fullPath.Split("/option/")[0];
						if(gamePath.Contains("/icon/"))
							gamePath = gamePath.Replace("/icon/", Regex.Match(gamePath, @"(/icon/\d\d\d)").Value + "000/");
						writeTex(tex, fullPath, gamePath, modid);
					}
					
					foreach(KeyValuePair<string, Dir> d in dir.dirs)
						walkDirAccent(d.Value, fullPath == null ? d.Key : (fullPath + "/" + d.Key), modid);
				}
				
				foreach(Mod mod in mods.Values)
					if(mod.id != "base" && main.config.modOptions[mod.id].enabled)
						walkDirAccent(mod.dir.dirs["elements_" + main.config.style], null, mod.id);
				walkDirAccent(dirAccent.dirs["elements_" + main.config.style], null, "base");
				
				if(!main.config.accentOnly) {
					void walkDirMain(Dir dir, string fullPath) {
						if(dir.files.Count > 0) {
							foreach(KeyValuePair<string, (string, string)> file in dir.files) {
								if(!file.Key.Contains(".dds"))
									continue;
								
								Tex tex = new Tex(File.ReadAllBytes(Path.GetFullPath(cachepath + file.Value.Item1)));
								
								string gamePath = fullPath.Split("/OPTIONS/")[0].ToLower().Replace("/hud/", "/uld/");
								if(gamePath.Contains("/icon/icon/"))
									gamePath = gamePath.Replace("/icon/icon/", Regex.Match(gamePath, @"(/icon/\d\d\d)").Value + "000/");
								writeTex(tex, fullPath, gamePath, "main");
							}
						}
						
						foreach(KeyValuePair<string, Dir> d in dir.dirs)
							walkDirMain(d.Value, fullPath == null ? d.Key : (fullPath + "/" + d.Key));
					}
					
					walkDirMain(dirMaster.dirs["4K resolution"].dirs[char.ToUpper(main.config.style[0]) + main.config.style.Substring(1)].dirs["Saved"], null);
				}
				
				File.WriteAllText(Path.GetFullPath(penumbraPath + "/Material UI/meta.json"), JsonConvert.SerializeObject(meta, Formatting.Indented));
				
				GC.Collect();
				
				busy = false;
			});
		}
	}
}