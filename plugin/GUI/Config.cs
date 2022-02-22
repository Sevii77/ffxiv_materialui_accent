using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Logging;

using Aetherment.Util;
using System.Threading.Tasks;

namespace Aetherment.GUI {
	internal partial class UI {
		private List<Mod> installedMods = new();
		private Dictionary<Mod, DateTime> applyTimes = new();
		
		private void DrawConfig() {
			ImGui.Text("TODO: make fancy");
			lock(installedMods)
				foreach(var mod in installedMods) {
					ImGui.PushID(mod.ID);
					ImGui.Text(mod.Name);
					if(ImGui.Button("Remove"))
						Installer.DeleteMod(mod);
					
					bool apply = false;
					foreach(var option in mod.Options)
						apply = DrawOption(option) || apply;
					
					if(ImGui.Button("Reset")) {
						apply = true;
						
						foreach(var option in mod.Options)
							switch(option) {
								case Mod.Option.Color clr:
									clr.Value = clr.Default;
									break;
							}
					}
					
					if(apply) {
						if(applyTimes.Count == 0)
							Task.Run(async() => {
								await Task.Delay(100);
								while(applyTimes.Count > 0) {
									if(!Installer.InstallStatus.Busy)
										foreach(var apply in applyTimes)
											if((DateTime.UtcNow - apply.Value).Seconds > 2) {
												applyTimes.Remove(apply.Key);
												PluginLog.Log("Applying");
												mod.SaveConfig();
												Installer.Apply(apply.Key, true);
												break;
											}
									await Task.Delay(100);
								}
							});
						applyTimes[mod] = DateTime.UtcNow;
					}
					
					ImGuiAeth.Offset(0, 20);
					ImGui.PopID();
				}
		}
		
		private bool DrawOption(Mod.Option option) {
			Vector4 clr;
			bool b = false;
			
			switch(option) {
				case Mod.Option.RGBA rgba:
					clr = rgba.Value;
					b = ImGui.ColorEdit4(rgba.Name, ref clr, ImGuiColorEditFlags.PickerHueWheel | ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar);
					rgba.Value = clr;
					break;
				case Mod.Option.RGB rgb:
					if(Aetherment.Config.ForceColor4) {
						clr = rgb.Value;
						b = ImGui.ColorEdit4(rgb.Name, ref clr, ImGuiColorEditFlags.PickerHueWheel | ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar);
						rgb.Value = clr;
					} else {
						clr = rgb.Value;
						b = ImGui.ColorEdit4(rgb.Name, ref clr, ImGuiColorEditFlags.PickerHueWheel | ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
						rgb.Value = clr;
					}
					break;
			}
			
			return b;
		}
		
		public void AddLocalMod(string id) {
			if(installedMods.Exists(x => x.ID == id))
				return;
			
			var mod = Mod.GetModLocal(id);
			if(mod != null)
				lock(installedMods)
					installedMods.Add(mod);
		}
		
		public void DeleteLocalMod(string id) {
			installedMods.RemoveAll(x => x.ID == id);
		}
	}
}