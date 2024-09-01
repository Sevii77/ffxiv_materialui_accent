using Dalamud.Game.Command;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.IO;
using Dalamud.Plugin.Services;
using Dalamud.Logging;
using Dalamud.IoC;
using Dalamud.Logging.Internal;

namespace MaterialUI {
	public class MaterialUI : IDalamudPlugin {
		public string Name => "Material UI";
		private const string command = "/materialui";
		
		public string penumbraIssue {get; private set;} = null;
		
		public IDalamudPluginInterface pluginInterface {get; private set;}
		public ICommandManager commandManager {get; private set;}
        public ModuleLog moduleLog { get; private set; }
        public UI ui {get; private set;}
		public Config config {get; private set;}
		public Updater updater {get; private set;}
		
		public MaterialUI(IDalamudPluginInterface pluginInterface, ICommandManager commandManager) {
			this.pluginInterface = pluginInterface;
			this.commandManager = commandManager;
			
			config = pluginInterface.GetPluginConfig() as Config ?? new Config();
			updater = new Updater(this);
			ui = new UI(this);
			
			commandManager.AddHandler(command, new CommandInfo(OnCommand) {
				HelpMessage = "Opens the Material UI configuration window"
			});
			
			if(config.openOnStart)
				ui.settingsVisible = true;
			
			Task.Run(async() => {
				for(int i = 0; i < 15; i++) {
					CheckPenumbra();
					if(penumbraIssue == null) {
						updater.Update();
						
						break;
					}
					
					await Task.Delay(1000);
				}
			});
		}
		
		public void Dispose() {
			commandManager.RemoveHandler(command);
			ui.Dispose();
		}
		
		public void CheckPenumbra() {
			try {
				pluginInterface.GetIpcSubscriber<(int, int)>("Penumbra.ApiVersions").InvokeFunc();
			} catch(Exception e) {
				moduleLog.Error("Penumbra.ApiVersions failed", e);
				penumbraIssue = "Penumbra not found.";
				
				return;
			}
			
			// string penumbraConfigPath = $"{pluginInterface.ConfigFile.DirectoryName}/Penumbra.json";
			// if (!File.Exists(penumbraConfigPath)) {
			// 	penumbraIssue = "Can't find Penumbra Config.";
			// 	
			// 	return;
			// }
			
			string penumbraPath = pluginInterface.GetIpcSubscriber<string>("Penumbra.GetModDirectory").InvokeFunc();
			if(penumbraPath == "") {
				penumbraIssue = "Penumbra Mod Directory has not been set.";
				
				return;
			}
			
			penumbraIssue = null;
		}
		
		private void OnCommand(string cmd, string args) {
			if(cmd == command)
				ui.settingsVisible = !ui.settingsVisible;
		}
	}
}