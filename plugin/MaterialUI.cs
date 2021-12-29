using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;

namespace MaterialUI {
	public class MaterialUI : IDalamudPlugin {
		public string Name => "Material UI";
		private const string command = "/materialui";
		
		public bool penumbraFound {get; private set;} = false;
		
		public DalamudPluginInterface pluginInterface {get; private set;}
		public CommandManager commandManager {get; private set;}
		public UI ui {get; private set;}
		public Config config {get; private set;}
		public Updater updater {get; private set;}
		
		public MaterialUI(DalamudPluginInterface pluginInterface, CommandManager commandManager) {
			this.pluginInterface = pluginInterface;
			this.commandManager = commandManager;
			
			config = pluginInterface.GetPluginConfig() as Config ?? new Config();
			updater = new Updater(this);
			ui = new UI(this);
			
			commandManager.AddHandler(command, new CommandInfo(OnCommand) {
				HelpMessage = "Opens the Material UI configuration window"
			});
			
			CheckPenumbra();
			updater.Update();
		}
		
		public void Dispose() {
			commandManager.RemoveHandler(command);
			ui.Dispose();
		}
		
		public void CheckPenumbra() {
			try {
				pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion").InvokeFunc();
				
				penumbraFound = true;
			} catch(Exception e) {
				penumbraFound = false;
			}
		}
		
		private void OnCommand(string cmd, string args) {
			if(cmd == command)
				ui.settingsVisible = !ui.settingsVisible;
		}
	}
}