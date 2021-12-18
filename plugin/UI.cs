using Dalamud.Plugin;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace MaterialUI {
	public class UI : IDisposable {
		public bool settingsVisible = false;
		public bool noticeVisible = false;
		
		private string noticeText = "";
		
		private MaterialUI main;
		
		private bool openOnStart;
		private bool accentOnly;
		private Vector3 colorAccent;
		private string localInput = "";
		private string repoInput = "";
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
			
			if(main.updater.busy) {
				ImGui.Text(main.updater.statusText);
			} else {
				try {
					main.pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion").InvokeFunc();
				} catch(Exception e) {
					ImGui.Text("Penumbra is not installed.");
					ImGui.End();
					
					return;
				}
				
				if(!main.updater.mods.ContainsKey("base")) {
					ImGui.End();
					
					return;
				}
				
				ImGui.BeginTabBar("MaterialUISettings");
				
				if(ImGui.BeginTabItem("Main")) {
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
						ImGui.ColorEdit3(main.updater.mods["base"].options.colorOptions[i].name, ref colorOptions[i], ImGuiColorEditFlags.NoInputs);
					}
					
					if(ImGui.Button("Apply")) {
						main.config.firstTime = false;
						main.config.color = colorAccent;
						main.config.accentOnly = accentOnly;
						for(int i = 0; i < colorOptions.Length; i++) {
							main.config.colorOptions[main.updater.mods["base"].options.colorOptions[i].id] = colorOptions[i];
						}
						
						main.pluginInterface.SavePluginConfig(main.config);
						main.updater.Apply();
					}
					
					ImGui.SameLine();
					if(ImGui.Button("Reset")) {
						colorAccent = new Vector3(99 / 255f, 60 / 255f, 181 / 255f);
						main.config.color = colorAccent;
						
						for(int i = 0; i < main.updater.mods["base"].options.colorOptions.Length; i++) {
							OptionColor option = main.updater.mods["base"].options.colorOptions[i];
							Vector3 clr = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
							
							main.config.colorOptions[option.id] = clr;
							colorOptions[i] = clr;
						}
						
						main.pluginInterface.SavePluginConfig(main.config);
						main.updater.Apply();
					}
					
					ImGui.Separator();
					ImGui.TextWrapped("Be sure UI Resolution is set to 4K. System Configuration > Graphics Settings.\n\nAfter applying be sure to Rediscover Mods and enable Material UI in Penumbra. A relog is also required. (some textures might still not update)");
					
					ImGui.EndTabItem();
				}
				
				if(ImGui.BeginTabItem("Mod Browser")) {
					ImGui.BeginChild("MaterialUIModBrowser");
					
					foreach(KeyValuePair<string, Mod> mod in main.updater.mods) {
						string id = mod.Key;
						if(id == "base")
							continue;
						
						Options options = mod.Value.options;
						Dir dir = mod.Value.dir;
						var preview = mod.Value.preview;
						
						ImGui.PushID("Mod" + id);
						if(ImGui.Checkbox("", ref main.config.modOptions[id].enabled)) {
							main.pluginInterface.SavePluginConfig(main.config);
							main.updater.UpdateCache();
						}
						
						ImGui.SameLine();
						bool tree = ImGui.TreeNode(options.name);
						ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(15, 0));
						ImGui.SameLine();
						ImGui.TextDisabled("by " + options.author);
						ImGui.PopStyleVar();
						if(ImGui.IsItemHovered())
							ImGui.SetTooltip(options.authorContact);
						
						if(tree) {
							ImGui.TextWrapped(options.description);
							ImGui.Separator();
							
							ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
							
							if(preview != null) {
								if(ImGui.TreeNode("Preview")) {
									float scale = (ImGui.GetWindowContentRegionWidth() - 45) / preview.Width;
									
									ImGui.Image(preview.ImGuiHandle, new Vector2(preview.Width * scale, preview.Height * scale));
									ImGui.TreePop();
								}
							}
							
							ImGui.PopStyleVar();
							ImGui.Separator();
							ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
							
							if(ImGui.TreeNode("Options")) {
								foreach(OptionPenumbra option in options.penumbraOptions) {
									ImGui.Text(option.name);
									
									int i = 0;
									int count = option.options.Count;
									foreach(string suboption in option.options.Keys) {
										i++;
										ImGui.Text((i == count ? "└ " : "├ ") + suboption);
									}
								}
								
								ImGui.TreePop();
							}
							
							ImGui.PopStyleVar();
							ImGui.Separator();
							ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
							
							if(ImGui.TreeNode("Files")) {
								Dir uld = dir.GetPathDir(string.Format("elements_{0}/ui/uld", main.config.style));
								if(uld != null) {
									foreach(string f in uld.dirs.Keys) {
										ImGui.Text(f);
									}
								}
								
								Dir icon = dir.GetPathDir(string.Format("elements_{0}/ui/icon", main.config.style));
								if(icon != null) {
									foreach(string f in icon.dirs.Keys) {
										ImGui.Text(f);
									}
								}
								
								ImGui.TreePop();
							}
							
							ImGui.PopStyleVar();
							ImGui.Separator();
							
							foreach(OptionColor option in options.colorOptions) {
								Vector3 color = main.config.modOptions[id].colors[option.id];
								ImGui.ColorEdit3(option.name, ref color, ImGuiColorEditFlags.NoInputs);
								if(color != main.config.modOptions[id].colors[option.id]) {
									main.config.modOptions[id].colors[option.id] = color;
									main.pluginInterface.SavePluginConfig(main.config);
								}
							}
							
							if(options.colorOptions.Length > 0)
								if(ImGui.Button("Reset"))
									foreach(OptionColor option in options.colorOptions) {
										main.config.modOptions[id].colors[option.id] = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
										main.pluginInterface.SavePluginConfig(main.config);
									}
							
							ImGui.TreePop();
						}
						
						ImGui.Separator();
						ImGui.PopID();
					}
					
					ImGui.EndChild();
					ImGui.EndTabItem();
				}
				
				if(ImGui.BeginTabItem("Advanced")) {
					ImGui.BeginChild("MaterialUIAdvanced");
					
					{ // Local Mods
						ImGui.Text("Local Mods");
						ImGui.TextWrapped("Path to mod directory, used for development");
						
						ImGui.PushID("localPathEnabled");
						if(ImGui.Checkbox("", ref main.config.localEnabled))
							main.pluginInterface.SavePluginConfig(main.config);
						ImGui.PopID();
						ImGui.PushID("localPathInput");
						ImGui.SameLine();
						ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionWidth());
						if(ImGui.InputText("", ref main.config.localPath, 200))
							main.pluginInterface.SavePluginConfig(main.config);
						ImGui.PopID();
					}
					
					ImGui.Separator();
					
					{ // Mod repos
						ImGui.Text("Mod Repositories (User/Repo)");
						
						ImGui.Button("  ");
						ImGui.SameLine();
						ImGui.Text(Updater.repoAccent);
						
						foreach(string repo in main.config.thirdPartyModRepos) {
							ImGui.PushID("modRepo" + repo);
							if(ImGui.Button("-")) {
								main.config.thirdPartyModRepos.Remove(repo);
								main.pluginInterface.SavePluginConfig(main.config);
								
								break;
							}
							ImGui.PopID();
							ImGui.SameLine();
							ImGui.Text(repo);
						}
						
						if(ImGui.Button("+")) {
							main.config.thirdPartyModRepos.Add(repoInput);
							repoInput = "";
							main.pluginInterface.SavePluginConfig(main.config);
						}
						ImGui.SameLine();
						ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionWidth());
						ImGui.InputText("", ref repoInput, 200);
					}
					
					ImGui.Separator();
					
					if(ImGui.Button("Reload Mods"))
						main.updater.LoadMods();
					
					ImGui.EndChild();
					ImGui.EndTabItem();
				}
				
				ImGui.EndTabBar();
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