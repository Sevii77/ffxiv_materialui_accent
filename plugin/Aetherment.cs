using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using ImGuiNET;
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

using Aetherment.Util;
using Aetherment.GUI;

namespace Aetherment {
	public class Aetherment : IDalamudPlugin {
		public string Name => "Aetherment";
		private const string command = "/aetherment";
		private const string commandAlt = "/materialui";
		
		[PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface Interface   {get; private set;} = null!;
		[PluginService][RequiredVersion("1.0")] public static CommandManager         Commands    {get; private set;} = null!;
		[PluginService][RequiredVersion("1.0")] public static GameGui                GameGui     {get; private set;} = null!;
		
		internal static UI ui;
		internal static List<GitHub.RepoInfo> repos;
		
		public Aetherment() {
			GitHub.ClearCache();
			// TODO: turn this into config
			repos = new List<GitHub.RepoInfo>{new GitHub.RepoInfo("Sevii77", "ffxiv_materialui_accent", "v2")};
			
			ui = new UI();
			
			
			Commands.AddHandler(command, new CommandInfo(OnCommand) {
				HelpMessage = "Open Aetherment menu"
			});
			Commands.AddHandler(commandAlt, new CommandInfo(OnCommand) {
				HelpMessage = "Alternative for /aetherment"
			});
		}
		
		public void Dispose() {
			Commands.RemoveHandler(command);
			Commands.RemoveHandler(commandAlt);
		}
		
		private void OnCommand(string cmd, string args) {
			if(cmd == command || cmd == commandAlt)
				ui.Show();
		}
	}
}