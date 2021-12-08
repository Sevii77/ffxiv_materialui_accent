using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;

namespace MaterialUI {
	public class UI : IDisposable {
		public bool settingsVisible = false;
		public bool noticeVisible = false;
		
		private string noticeText = "";
		
		private MaterialUI main;
		
		private bool openOnStart;
		private bool accentOnly;
		private Vector3 colorAccent;
		public Vector3[] colorOptions;
		
		public UI(MaterialUI main) {
			this.main = main;
			
			// settingsVisible = true;
			// noticeVisible = true;
			
			openOnStart = main.config.openOnStart;
			accentOnly = main.config.accentOnly;
			colorAccent = main.config.color;
			colorOptions = new Vector3[0];
			
			main.pluginInterface.UiBuilder.Draw += Draw;
		}
		
		public void Dispose() {
			main.pluginInterface.UiBuilder.Draw -= Draw;
		}
		
		public void ShowNotice(string text) {
			noticeText = text;
			noticeVisible = true;
		}
		
		private void Draw() {
			if(settingsVisible)
				DrawSettings();
			
			if(noticeVisible)
				DrawNotice();
		}
		
		private void DrawSettings() {
			ImGui.SetNextWindowSize(new Vector2(300, 450), ImGuiCond.FirstUseEver);
			ImGui.Begin("Material UI Settings", ref settingsVisible);
			
			if(main.updater.downloading) {
				ImGui.Text(main.updater.statusText);
			} else {
				try {
					main.pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion").InvokeFunc();
					
					if(ImGui.Checkbox("Open on Start", ref openOnStart)) {
						main.config.openOnStart = openOnStart;
						main.pluginInterface.SavePluginConfig(main.config);
					}
					
					ImGui.Separator();
					
					ImGui.Checkbox("Colors Only", ref accentOnly);
					if(ImGui.IsItemHovered())
						ImGui.SetTooltip("Only installs the recolored textures instead of everything. For those that have a frankenstein setup, using both TexTools and Penumbra");
						
					ImGui.Separator();
					
					ImGui.ColorEdit3("Accent", ref colorAccent, ImGuiColorEditFlags.NoInputs);
					for(int i = 0; i < colorOptions.Length; i++) {
						ImGui.ColorEdit3(main.updater.options.colorOptions[i].name, ref colorOptions[i], ImGuiColorEditFlags.NoInputs);
					}
					
					if(ImGui.Button("Apply")) {
						main.config.firstTime = false;
						main.config.color = colorAccent;
						main.config.accentOnly = accentOnly;
						for(int i = 0; i < colorOptions.Length; i++) {
							main.config.colorOptions[main.updater.options.colorOptions[i].id] = colorOptions[i];
						}
						
						main.pluginInterface.SavePluginConfig(main.config);
						main.updater.Apply();
					}
					
					ImGui.SameLine();
					
					if(ImGui.Button("Reset")) {
						colorAccent = new Vector3(99 / 255f, 60 / 255f, 181 / 255f);
						main.config.color = colorAccent;
						
						for(int i = 0; i < main.updater.options.colorOptions.Length; i++) {
							OptionColor option = main.updater.options.colorOptions[i];
							Vector3 clr = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
							
							main.config.colorOptions[option.id] = clr;
							colorOptions[i] = clr;
						}
						
						main.pluginInterface.SavePluginConfig(main.config);
						main.updater.Apply();
					}
					
					ImGui.Separator();
					
					ImGui.TextWrapped("Be sure UI Resolution is set to 4K. System Configuration > Graphics Settings.\n\nAfter applying be sure to Rediscover Mods and enable Material UI in Penumbra. A relog is also required. (some textures might still not update)");
				} catch(Exception e) {
					ImGui.Text("Penumbra is not installed.");
				}
			}
			
			ImGui.End();
		}
		
		private void DrawNotice() {
			ImGui.Begin("Material UI Notice", ref noticeVisible);
			ImGui.Text(noticeText);
			ImGui.End();
		}
	}
}