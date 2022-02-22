using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

using ImGuiNET;
using ImGuiScene;
using Dalamud;
using Dalamud.IoC;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;

using Aetherment.GUI;
using Aetherment.Util;
using System.Threading.Tasks;
using System.Diagnostics;

// TODO save mod config files locally and use that for the config tab

namespace Aetherment {
	[Serializable]
	public class Config {
		public int Version = 0;
		
		public List<string> InstalledMods = new();
		
		public bool AutoUpdate = true;
		public bool LinkOptions = true;
		
		public bool AdvancedMode = false;
		public bool ForceColor4 = false;
		public bool LocalMods = false;
		public string LocalModsPath = "";
		public List<GitHub.RepoInfo> Repos = new();
	}
	
	public class Aetherment : IDalamudPlugin {
		public string Name => "Aetherment";
		private const string command = "/aetherment";
		private const string commandAlt = "/materialui";
		
		[PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface Interface {get; private set;} = null!;
		[PluginService][RequiredVersion("1.0")] public static CommandManager         Commands  {get; private set;} = null!;
		[PluginService][RequiredVersion("1.0")] public static GameGui                GameGui   {get; private set;} = null!;
		[PluginService][RequiredVersion("1.0")] public static TitleScreenMenu        TitleMenu {get; private set;} = null!;
		
		internal static Config Config;
		internal static UI Ui;
		
		internal static Dictionary<string, TextureWrap> Textures = new();
		
		public Aetherment() {
			Installer.Initialize();
			foreach(var file in new DirectoryInfo(Interface.AssemblyLocation.DirectoryName + "/assets").EnumerateFiles())
				Textures[file.Name] = Interface.UiBuilder.LoadImage(file.FullName);
			
			var path = $"{Aetherment.Interface.ConfigDirectory.FullName}/config.json";
			Config = File.Exists(path) ? JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)) : new Config();
			Config.Repos.Insert(0, new GitHub.RepoInfo("Sevii77", "ffxiv_materialui_accent", "v2"));
			Ui = new UI();
			
			Commands.AddHandler(command, new CommandInfo(OnCommand) {
				HelpMessage = "Open Aetherment menu"
			});
			Commands.AddHandler(commandAlt, new CommandInfo(OnCommand) {
				HelpMessage = "Alternative for /aetherment"
			});
		}
		
		public void Dispose() {
			Installer.Dispose();
			Ui.Dispose();
			Commands.RemoveHandler(command);
			Commands.RemoveHandler(commandAlt);
			
			foreach(var texture in Textures.Values)
				texture.Dispose();
		}
		
		public static void AddInstalledMod(string id) {
			if(!Config.InstalledMods.Contains(id))
				Config.InstalledMods.Add(id);
			
			Aetherment.Ui.AddLocalMod(id);
			SaveConfig();
		}
		
		public static void DeleteInstalledMod(string id) {
			Config.InstalledMods.Remove(id);
			Aetherment.Ui.DeleteLocalMod(id);
			SaveConfig();
		}
		
		public static void SaveConfig() {
			Config.Repos.RemoveAt(0); // lol idc anymore for now
			File.WriteAllText($"{Aetherment.Interface.ConfigDirectory.FullName}/config.json", JsonConvert.SerializeObject(Config));
			Config.Repos.Insert(0, new GitHub.RepoInfo("Sevii77", "ffxiv_materialui_accent", "v2"));
		}
		
		private void OnCommand(string cmd, string args) {
			if(cmd == command || cmd == commandAlt)
				Ui.Show();
		}
	}
}