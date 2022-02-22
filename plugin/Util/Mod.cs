using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using ImGuiScene;
using Dalamud.Logging;

namespace Aetherment.Util {
	public partial class Mod : IDisposable {
		public class DalamudStyle {
			
		}
		
		public interface IOption<T> {
			T Default {get; set;}
			T Value {get; set;}
		}
		
		public interface IJsonWritable {
			public void WriteJson(JsonTextWriter writer);
			public void ReadJson(JObject val);
		}
		
		public class Option {
			public class Customizable : Option, IJsonWritable {
				[JsonProperty("id")] public string ID {get; private set;}
				public virtual void WriteJson(JsonTextWriter writer) {}
				public virtual void ReadJson(JObject val) {}
			}
			
			// ----------------------------------------
			
			public class Color : Customizable {
				[JsonIgnore] public Vector4 Default {get; set;}
				[JsonIgnore] public Vector4 Value {get; set;}
				
				public override void WriteJson(JsonTextWriter writer) {
					writer.WritePropertyName(ID);
						// writer.WriteValue(Value);
						writer.WriteStartObject();
							writer.WritePropertyName("X");
								writer.WriteValue(Value.X);
							writer.WritePropertyName("Y");
								writer.WriteValue(Value.Y);
							writer.WritePropertyName("Z");
								writer.WriteValue(Value.Z);
							writer.WritePropertyName("W");
								writer.WriteValue(Value.W);
						writer.WriteEndObject();
				}
				
				public override void ReadJson(JObject val) {
					Value = val.ToObject<Vector4>();
				}
			}
			
			public class RGBA : Color, IOption<Vector4> {
				[JsonExtensionData]
				private IDictionary<string, JToken> data;
				
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					var def = data["default"].ToObject<List<int>>();
					Default = new Vector4(def[0] / 255f, def[1] / 255f, def[2] / 255f, def[3] / 255f);
					Value = new Vector4(def[0] / 255f, def[1] / 255f, def[2] / 255f, def[3] / 255f);
				}
			}
			
			public class RGB : Color, IOption<Vector4> {
				[JsonExtensionData]
				private IDictionary<string, JToken> data;
				
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					var def = data["default"].ToObject<List<int>>();
					Default = new Vector4(def[0] / 255f, def[1] / 255f, def[2] / 255f, 1f);
					Value = new Vector4(def[0] / 255f, def[1] / 255f, def[2] / 255f, 1f);
				}
			}
			
			public class Grayscale : Color, IOption<Vector4> {
				[JsonExtensionData]
				private IDictionary<string, JToken> data;
				
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					var def = (int)data["default"] / 255f;
					Default = new Vector4(def, def, def, 1);
					Value = new Vector4(def, def, def, 1);
				}
			}
			
			public class Opacity : Color, IOption<Vector4> {
				[JsonExtensionData]
				private IDictionary<string, JToken> data;
				
				[OnDeserialized]
				private void OnDeserialized(StreamingContext context) {
					Default = new Vector4(1, 1, 1, (int)data["default"] / 255f);
					Value = new Vector4(1, 1, 1, (int)data["default"] / 255f);
				}
			}
			
			// ----------------------------------------
			
			public class Penumbra : Option, IOption<int> {
				[JsonIgnore] public int Default {get; set;} = 0;
				[JsonIgnore] public int Value {get; set;} = 0;
				[JsonProperty("options")] public Dictionary<string, string[]> Options {get; private set;}
			}
			
			public class Single : Penumbra {}
			public class Multi : Penumbra {}
			
			// public class Single : Option, IOption<int> {
			// 	public int Default {get; set;} = 0;
			// 	public int Value {get; set;} = 0;
			// 	[JsonProperty("options")] public Dictionary<string, string[]> Options {get; private set;}
			// }
			
			// public class Multi : Option, IOption<int> {
			// 	public int Default {get; set;} = 0;
			// 	public int Value {get; set;} = 0;
			// 	[JsonProperty("options")] public Dictionary<string, string[]> Options {get; private set;}
			// }
			
			[JsonProperty("name")] public string Name {get; private set;}
		}
		
		private class OptionBinder : ISerializationBinder {
			public IList<Type> KnownTypes;
			
			public Type BindToType(string ass, string typ) {
				// if(typ == "multi" || typ == "single")
				// 	typ = "penumbra";
				
				return KnownTypes.SingleOrDefault(t => t.Name.ToLower() == typ);
			}
			
			public void BindToName(Type ser, out string ass, out string typ) {
				ass = null;
				typ = ser.Name.ToLower();
			}
		}
		
		// public ConfigMod Config {get; private set;} = new();
		public GitHub.RepoInfo Repo {get; private set;}
		public bool Enabled {get; private set;} = false;
		public bool AutoUpdate {get; private set;} = true;
		public List<string> DisabledPaths {get; private set;} = new();
		public string Raw {get; private set;}
		
		public string ID {get; private set;} = "ID";
		public uint ID2 {get; private set;}
		[JsonProperty("author")] public string Author {get; private set;} = "Unknown";
		[JsonProperty("author_contact")] public string AuthorContact {get; private set;} = "";
		[JsonProperty("name")] public string Name {get; private set;} = "No Name";
		[JsonProperty("description")] public string Description {get; private set;} = "";
		// [JsonProperty("tags")] public List<string> Tags {get; private set;} = new();
		public List<string> Tags {get; private set;} = new();
		public List<string> TagsFancy {get; private set;} = new();
		[JsonProperty("links")] public string[] Links {get; private set;} = new string[0];
		[JsonProperty("dependencies")] public string[] Dependencies {get; private set;} = new string[0];
		[JsonProperty("options_inherit")] public string[] OptionsInherit {get; private set;} = new string[0];
		[JsonProperty("standalone")] public bool Standalone {get; private set;} = true;
		[JsonProperty("dalamud")] public Dictionary<string, DalamudStyle> Dalamud {get; private set;} = new();
		[JsonProperty("options")] public List<Option> Options {get; private set;} = new();
		public List<TextureWrap> Previews {get; private set;} = new();
		public Dir Files {get; private set;}
		public int Size {get; private set;}
		
		public void LoadConfig() {
			var path = $"{Aetherment.Interface.ConfigDirectory.FullName}/configs/{ID}.json";
			if(!File.Exists(path))
				return;
			
			var config = JObject.Parse(File.ReadAllText(path));
			Repo = config["Repo"].ToObject<GitHub.RepoInfo>();
			AutoUpdate = (bool)config["AutoUpdate"];
			DisabledPaths = config["DisabledPaths"].ToObject<List<string>>();
			Raw = (string)config["RawConfig"];
			
			foreach(var option in config["OptionValues"].ToObject<Dictionary<string, JObject>>())
				((Option.Customizable)Options.First(x => x is Option.Customizable && ((Option.Customizable)x).ID == option.Key)).ReadJson(option.Value);
		}
		
		public void SaveConfig() {
			var sw = new StringWriter();
			var writer = new JsonTextWriter(sw);
			
			writer.WriteStartObject();
				writer.WritePropertyName("Repo");
					writer.WriteStartObject();
						writer.WritePropertyName("Author");
							writer.WriteValue(Repo.Author);
						writer.WritePropertyName("Repo");
							writer.WriteValue(Repo.Repo);
						writer.WritePropertyName("Branch");
							writer.WriteValue(Repo.Branch);
					writer.WriteEndObject();
				
				writer.WritePropertyName("AutoUpdate");
					writer.WriteValue(AutoUpdate);
				
				writer.WritePropertyName("DisabledPaths");
					writer.WriteStartArray();
					foreach(string p in DisabledPaths)
						writer.WriteValue(p);
					writer.WriteEndArray();
				
				writer.WritePropertyName("OptionValues");
					writer.WriteStartObject();
					foreach(Option option in Options)
						if(option is Option.Customizable)
							((Option.Customizable)option).WriteJson(writer);
					writer.WriteEndObject();
				
				writer.WritePropertyName("RawConfig");
					writer.WriteValue(Raw);
			writer.WriteEndObject();
			
			var path = $"{Aetherment.Interface.ConfigDirectory.FullName}/configs/{ID}.json";
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, sw.ToString());
		}
		
		public void LoadPreview() {
			if(Files.Dirs["previews"].Files.Count == 0 || Previews.Count > 0)
				return;
			
			Task.Run(async() => {
				foreach(Dir.File file in Files.Dirs["previews"].Files.Values) {
					Previews.Add(Aetherment.Interface.UiBuilder.LoadImage(await Installer.GetByteArray(file.Path)));
					break;
				}
			});
		}
		
		public void LoadPreviews() {
			if(Files.Dirs["previews"].Files.Count == Previews.Count)
				return;
			
			DisposePreviews();
			
			Task.Run(async() => {
				foreach(Dir.File file in Files.Dirs["previews"].Files.Values)
					Previews.Add(Aetherment.Interface.UiBuilder.LoadImage(await Installer.GetByteArray(file.Path)));
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
				{"local", "Local Mod"},
			},
			
			new() {
				{"ui", "UI"},
				{"4k", "4K UI Resolution"},
				{"hd", "HD UI Resolution"},
				{"gauge", "Gauges"},
				{"icon", "Icons"},
				{"map", "Maps"},
				{"window", "Windows"},
				{"cursor", "Target Cursor"}
			},
			
			// new() {
			// 	{"vfx", "VFX Effects"}
			// }
		};
		
		private static Dictionary<string, Mod> modCache = new();
		private static uint idCounter = 489657309;
		
		public static Mod GetModLocal(string id) {
			var path = $"{Aetherment.Interface.ConfigDirectory.FullName}/configs/{id}.json";
			if(!File.Exists(path))
				return null;
			
			// This is dumb
			var mod = JsonConvert.DeserializeObject<Mod>((string)JObject.Parse(File.ReadAllText(path))["RawConfig"], new JsonSerializerSettings{
				TypeNameHandling = TypeNameHandling.Objects,
				SerializationBinder = new OptionBinder() {KnownTypes = new List<Type>() {
					typeof(Option.RGBA),
					typeof(Option.RGB),
					typeof(Option.Grayscale),
					typeof(Option.Opacity),
					// typeof(Option.Penumbra)
					typeof(Option.Single),
					typeof(Option.Multi)
				}}
			});
			
			mod.ID = id;
			mod.LoadConfig();
			
			return mod;
		}
		
		public static async Task<Mod> GetMod(GitHub.RepoInfo repo, string id) {
			await CacheMod(repo, id);
			
			if(!modCache.ContainsKey(id))
				return null;
			
			return modCache[id];
		}
		
		public static async Task<List<Mod>> GetMods(string search = null, List<string> tags = null) {
		// public static async Task<List<Mod>> GetMods(List<GitHub.RepoInfo> repos, string search = null, List<string> tags = null) {
			await CacheMods();
			// await CacheMods(repos);
			
			if((search == null || search == "") && tags == null)
				return modCache.Values.ToList();
			
			if(search != null)
				search = search.ToLower();
			
			// TODO: use regex to search by full words (for description only mby)
			List<Mod> mods = new();
			foreach(var mod in modCache.Values) {
				bool successSearch = search == null || search == "" || mod.Name.ToLower().Contains(search) || mod.Author.ToLower() == search || mod.AuthorContact.ToLower() == search;
				bool successTags = tags == null;
				
				if(!successTags) {
					successTags = true;
					
					foreach(var tag in tags)
						if(!mod.Tags.Contains(tag.ToLower())) {
						// if(Array.IndexOf(mod.Tags, tag.ToLower()) == -1) {
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
			
			var dir = (await GitHub.GetRepo(repo))?.GetPathDir("mods/" + id);
			if(repo.Author == "") {
				dir = new("", "");
				void walk2(DirectoryInfo dir, Dir dir2) {
					foreach(var sub in dir.EnumerateDirectories())
						walk2(sub, dir2.AddDir(sub.Name, sub.FullName.GetHashCode().ToString("X")));
					
					foreach(var file in dir.EnumerateFiles())
						dir2.AddFile(file.Name, file.FullName.GetHashCode().ToString("X"), file.FullName, (int)new FileInfo(file.FullName).Length);
				}
				walk2(new DirectoryInfo($"{Aetherment.Config.LocalModsPath}/{id}"), dir);
			}
			
			if(dir == null)
				return;
			
			var config = Regex.Replace(await Installer.GetString(dir.Files["config.json"].Path), "\\s//[^\n]*", "");
			// var config = Regex.Replace(Regex.Replace(await Installer.GetString(dir.Files["config.json"].Path), "\\s//[^\n]*", ""), "\"type\"", "\"$type\"");
			var mod = JsonConvert.DeserializeObject<Mod>(config, new JsonSerializerSettings{
				TypeNameHandling = TypeNameHandling.Objects,
				SerializationBinder = new OptionBinder() {KnownTypes = new List<Type>() {
					typeof(Option.RGBA),
					typeof(Option.RGB),
					typeof(Option.Grayscale),
					typeof(Option.Opacity),
					// typeof(Option.Penumbra)
					typeof(Option.Single),
					typeof(Option.Multi)
				}}
			});
			mod.ID = id;
			mod.ID2 = idCounter;
			mod.Files = dir;
			mod.Size = 0;
			mod.Repo = repo;
			mod.Raw = config;
			
			mod.Tags.Clear(); // TODO: remove tags from config file, probably
			TagGrabber.AddTags(mod);
			
			if(repo.Author == "")
				mod.Tags.Add("local");
			
			foreach(var tag in mod.Tags)
				foreach(var names in TagNames)
					if(names.ContainsKey(tag)) {
						mod.TagsFancy.Add(names[tag]);
						
						break;
					}
			
			void walk(Dir dir) {
				foreach(var sub in dir.Dirs.Values)
					walk(sub);
				
				foreach(var file in dir.Files.Values)
					mod.Size += file.Size;
			}
			walk(mod.Files.Dirs["files"]);
			
			mod.LoadPreview();
			
			modCache[id] = mod;
			idCounter++;
		}
		
		public static async Task CacheMods() {
		// public static async Task CacheMods(List<GitHub.RepoInfo> repos) {
			foreach(var repoinfo in Aetherment.Config.Repos)
			// foreach(GitHub.RepoInfo repoinfo in repos)
				foreach(string mod in (await GitHub.GetRepo(repoinfo)).Dirs["mods"].Dirs.Keys)
					await CacheMod(repoinfo, mod);
			
			if(Aetherment.Config.LocalMods)
				foreach(var dir in new DirectoryInfo(Aetherment.Config.LocalModsPath).EnumerateDirectories())
					await CacheMod(new GitHub.RepoInfo("", "", ""), dir.Name);
		}
		
		public static void ClearCache() {
			foreach(Mod mod in modCache.Values)
				mod.DisposePreviews();
			
			modCache = new();
		}
	}
}