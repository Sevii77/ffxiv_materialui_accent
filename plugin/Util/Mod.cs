using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ImGuiScene;
using Dalamud.Logging;

namespace Aetherment.Util {
	internal class Mod : IDisposable {
		internal class DalamudStyle {
			
		}
		
		internal class Option {
			internal class RGBA : Option {
				[JsonProperty("id")] public string ID {get; private set;}
				public Vector4 Default {get; protected set;}
				
				[JsonExtensionData]
				protected IDictionary<string, JToken> data;
				
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					var def = data["default"].ToObject<Dictionary<string, int>>();
					Default = new Vector4(def["r"] / 255f, def["g"] / 255f, def["b"] / 255f, def["a"] / 255f);
				}
			}
			
			internal class RGB : RGBA {
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					var def = data["default"].ToObject<Dictionary<string, int>>();
					Default = new Vector4(def["r"] / 255f, def["g"] / 255f, def["b"] / 255f, 1);
				}
			}
			
			internal class Grayscale : RGBA {
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					var def = (int)data["default"] / 255f;
					Default = new Vector4(def, def, def, 1);
				}
			}
			
			internal class Opacity : RGBA {
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					Default = new Vector4(1, 1, 1, (int)data["default"] / 255f);
				}
			}
			
			internal class Penumbra : Option {
				[JsonProperty("options")] public Dictionary<string, string[]> Options {get; private set;}
			}
			
			[JsonProperty("type")] public string Type {get; private set;}
			[JsonProperty("name")] public string Name {get; private set;}
		}
		
		public string ID {get; private set;} = "ID";
		public uint ID2 {get; private set;}
		[JsonProperty("author")] public string Author {get; private set;} = "Unknown";
		[JsonProperty("author_contact")] public string AuthorContact {get; private set;} = "";
		[JsonProperty("name")] public string Name {get; private set;} = "No Name";
		[JsonProperty("description")] public string Description {get; private set;} = "";
		[JsonProperty("tags")] public string[] Tags {get; private set;} = new string[0];
		public string[] TagsFancy {get; private set;} = new string[0];
		[JsonProperty("links")] public string[] Links {get; private set;} = new string[0];
		[JsonProperty("dependencies")] public string[][] Dependencies {get; private set;} = new string[0][];
		[JsonProperty("options_inherit")] public string[] OptionsInherit {get; private set;} = new string[0];
		[JsonProperty("standalone")] public bool Standalone {get; private set;} = true;
		[JsonProperty("dalamud")] public Dictionary<string, DalamudStyle> Dalamud {get; private set;} = new();
		[JsonProperty("options")] public List<Option> Options {get; private set;} = new();
		public List<TextureWrap> Previews {get; private set;} = new();
		public Dir Files {get; private set;}
		
		public void LoadPreview() {
			if(Files.dirs["previews"].files.Count == 0 || Previews.Count > 0)
				return;
			
			Task.Run(async() => {
				foreach(Dir.File file in Files.dirs["previews"].files.Values) {
					Previews.Add(Aetherment.Interface.UiBuilder.LoadImage(await GitHub.GetByteArray(file.path)));
					break;
				}
			});
		}
		
		public void LoadPreviews() {
			if(Files.dirs["previews"].files.Count == Previews.Count)
				return;
			
			DisposePreviews();
			
			Task.Run(async() => {
				foreach(Dir.File file in Files.dirs["previews"].files.Values)
					Previews.Add(Aetherment.Interface.UiBuilder.LoadImage(await GitHub.GetByteArray(file.path)));
			});
		}
		
		public void DisposePreviews() {
			foreach(TextureWrap preview in Previews)
				if(preview != null)
					preview.Dispose();
			
			Previews = new();
		}
		
		public void Dispose() {
			DisposePreviews();
		}
		
		// ---------------------------------------- //
		
		public static List<Dictionary<string, string>> TagNames = new() {
			new() {
				{"customizable", "Customizable"},
			},
			
			new() {
				{"4k", "4K UI Resolution"},
				{"hd", "HD UI Resolution"},
				{"ui", "UI"},
				{"theme", "Theme"},
				{"gauge", "Gauges"},
				{"icon", "Icons"},
				{"map", "Maps"},
				{"window", "Windows"},
				{"cursor", "Target Cursor"},
			},
			
			new() {
				{"vfx", "VFX Effects"}
			}
		};
		
		private static Dictionary<string, Mod> modCache = new();
		private static uint idCounter = 489657309;
		
		public static async Task<Mod> GetMod(GitHub.RepoInfo repo, string id) {
			await CacheMod(repo, id);
			
			if(!modCache.ContainsKey(id))
				return null;
			
			return modCache[id];
		}
		
		public static async Task<List<Mod>> GetMods(List<GitHub.RepoInfo> repos, string search = null, List<string> tags = null) {
			await CacheMods(repos);
			
			if((search == null || search == "") && tags == null)
				return modCache.Values.ToList();
			
			if(search != null)
				search = search.ToLower();
			
			// TODO: use regex to search by full words (for description only mby)
			List<Mod> mods = new();
			foreach(Mod mod in modCache.Values) {
				bool successSearch = search == null || search == "" || mod.Name.ToLower().Contains(search) || mod.Author.ToLower() == search || mod.AuthorContact.ToLower() == search;
				bool successTags = tags == null;
				
				if(!successTags) {
					successTags = true;
					
					foreach(string tag in tags)
						if(Array.IndexOf(mod.Tags, tag.ToLower()) == -1) {
							successTags = false;
							break;
						}
				}
				
				if(successSearch && successTags)
					mods.Add(mod);
			}
			
			return mods;
		}
		
		public static async Task CacheMod(GitHub.RepoInfo repo, string id) {
			if(modCache.ContainsKey(id))
				return;
			
			Dir dir = (await GitHub.GetRepo(repo)).GetPathDir("mods/" + id);
			if(dir == null)
				return;
			
			Mod mod = JsonConvert.DeserializeObject<Mod>(Regex.Replace(await GitHub.GetString(dir.files["config.json"].path), "\\s//[^\n]*", ""));
			mod.ID = id;
			mod.ID2 = idCounter;
			mod.Files = dir;
			mod.LoadPreview();
			
			mod.TagsFancy = new string[mod.Tags.Length];
			// foreach(string tag in mod.Tags)
			for(int i = 0; i < mod.Tags.Length; i++) {
				string tag = mod.Tags[i];
				foreach(Dictionary<string, string> names in TagNames)
					if(names.ContainsKey(tag)) {
						mod.TagsFancy[i] = names[tag];
						break;
					}
			}
			
			modCache[id] = mod;
			idCounter++;
		}
		
		public static async Task CacheMods(List<GitHub.RepoInfo> repos) {
			foreach(GitHub.RepoInfo repoinfo in repos)
				foreach(string mod in (await GitHub.GetRepo(repoinfo)).dirs["mods"].dirs.Keys)
					await CacheMod(repoinfo, mod);
		}
		
		public static void ClearCache() {
			foreach(Mod mod in modCache.Values)
				mod.DisposePreviews();
			
			modCache = new();
		}
	}
}