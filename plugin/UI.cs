using Dalamud.Plugin;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.IO;

namespace MaterialUI {
	public class UI : IDisposable {
		public bool settingsVisible = false;
		
		private bool noticeVisible = false;
		private List<string> noticeText;
		
		private MaterialUI main;
		
		private bool openOnStart;
		private bool accentOnly;
		private Vector3 colorAccent;
		private string repoInput = "";
		public Vector3[] colorOptions;
		
		public UI(MaterialUI main) {
			this.main = main;
			
			noticeText = new List<string>();
			
			openOnStart = main.config.openOnStart;
			accentOnly = main.config.accentOnly;
			colorAccent = main.config.color;
			colorOptions = new Vector3[0];
			
			main.pluginInterface.UiBuilder.Draw += Draw;
			main.pluginInterface.UiBuilder.OpenConfigUi += ShowSettings;
		}
		
		public void Dispose() {
			main.pluginInterface.UiBuilder.Draw -= Draw;
			main.pluginInterface.UiBuilder.OpenConfigUi -= ShowSettings;
		}
		
		public void ShowSettings() {
			settingsVisible = true;
		}
		
		public void ShowNotice(string text, bool force = false) {
			if(force)
				settingsVisible = true;
			
			noticeVisible = true;
			noticeText = new List<string>() {text};
		}
		
		public void ShowNotice(List<string> text, bool force = false) {
			if(force)
				settingsVisible = true;
			
			noticeVisible = true;
			noticeText = new List<string>(text);
		}
		
		public void CloseNotice() {
			noticeVisible = false;
		}
		
		private void Draw() {
			if(settingsVisible)
				DrawSettings();
		}
		
		private void DrawSettings() {
			ImGui.SetNextWindowSize(new Vector2(300, 450), ImGuiCond.FirstUseEver);
			ImGui.Begin("Material UI Settings", ref settingsVisible);
			
			if(noticeVisible) {
				ImGui.BeginChild("MaterialUINotice", new Vector2(ImGui.GetWindowContentRegionWidth(), ImGui.GetWindowHeight() - 60));
				foreach(string text in noticeText)
					ImGui.Text(text);
				ImGui.EndChild();
				
				if(ImGui.Button("Close"))
					CloseNotice();
			} else {
				if(main.penumbraIssue != null) {
					ImGui.Text(main.penumbraIssue);
					if(ImGui.Button("Retry")) {
						main.CheckPenumbra();
						main.updater.Update();
					}
					ImGui.End();
					
					return;
				}
				
				if(!main.updater.mods.ContainsKey("base")) {
					ImGui.Text("Failed loading remote data");
					if(ImGui.Button("Retry")) {
						main.CheckPenumbra();
						main.updater.Update();
					}
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
						main.updater.ApplyAsync();
					}
					
					ImGui.SameLine();
					if(ImGui.Button("Apply Dalamud Style")) {
						main.CheckPenumbra();
						if(main.penumbraIssue != null) {
							ImGui.End();
							
							return;
						}
						
						main.config.color = colorAccent;
						
						Dictionary<string, int> styleOverrides = new();
						foreach(Mod mod in main.updater.mods.Values) {
							string id = mod.id;
							if(id == "base")
								continue;
							
							if(main.config.modOptions[id].enabled && mod.options.styleOptions != null)
								foreach(KeyValuePair<string, int> val in mod.options.styleOptions)
									styleOverrides[val.Key] = val.Value;
						}
						
						DalamudStyle.Apply(main, styleOverrides);
					}
					
					ImGui.SameLine();
					if(ImGui.Button("Reset Colors")) {
						colorAccent = new Vector3(99 / 255f, 60 / 255f, 181 / 255f);
						main.config.color = colorAccent;
						
						for(int i = 0; i < main.updater.mods["base"].options.colorOptions.Length; i++) {
							OptionColor option = main.updater.mods["base"].options.colorOptions[i];
							Vector3 clr = new Vector3(option.@default.r / 255f, option.@default.g / 255f, option.@default.b / 255f);
							
							main.config.colorOptions[option.id] = clr;
							colorOptions[i] = clr;
						}
						
						main.pluginInterface.SavePluginConfig(main.config);
						main.updater.ApplyAsync();
					}
					
					ImGui.Separator();
					ImGui.TextWrapped("Be sure UI Resolution is set to 4K. System Configuration > Graphics Settings.\n\nAfter applying be sure to Rediscover Mods and enable Material UI in Penumbra. A relog is also required. (some textures might still not update)");
					
					ImGui.EndTabItem();
				}
				
				if(ImGui.BeginTabItem("Mod Browser")) {
					ImGui.BeginChild("MaterialUIModBrowser");
					ImGui.TextWrapped("You'll need to Apply in the main tab after selecting and configuring the desired mods");
					ImGui.Separator();
					
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
							
							if(options.penumbraOptions.Length > 0) {
								ImGui.Separator();
								ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
								if(ImGui.TreeNode("Options")) {
									ImGui.Text("Options that will be added to Penumbra");
									ImGui.Separator();
									
									foreach(OptionPenumbra option in options.penumbraOptions) {
										ImGui.Text(option.name);
										
										int i = 0;
										foreach(string suboption in option.options.Keys) {
											i++;
											ImGui.Text((i == option.options.Count ? "└ " : "├ ") + suboption);
										}
									}
									
									ImGui.TreePop();
								}
								ImGui.PopStyleVar();
							}
							
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
								if(ImGui.Button("Reset Colors"))
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
						ImGui.Text("Path to mod directory, used for development");
						
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
					
					ImGui.Separator();
					if(ImGui.Button("Check Integrity"))
						main.updater.Repair();
					
					// ImGui.Separator();
					// if(ImGui.Button("Dalamud test"))
					// 	DalamudStyle.Apply(main.config);
					
					ImGui.EndChild();
					ImGui.EndTabItem();
				}
				
				ImGui.EndTabBar();
			}
			
			ImGui.End();
		}
	}
}