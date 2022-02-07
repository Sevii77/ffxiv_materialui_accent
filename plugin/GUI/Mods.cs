using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Interface.Components;

using Aetherment.Util;

namespace Aetherment.GUI {
	internal partial class UI {
		private List<Mod> modsOpen;
		private string newestMod;
		
		private void DrawMods() {
			ImGui.BeginTabBar("AethermentMods");
			
			List<Mod> newOpen = new();
			foreach(Mod mod in modsOpen) {
				var open = true;
				if(ImGui.BeginTabItem(mod.Name, ref open, newestMod == mod.ID ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None)) {
					DrawModPage(mod);
					ImGui.EndTabItem();
				}
				
				if(open)
					newOpen.Add(mod);
			}
			modsOpen = newOpen;
			newestMod = null;
			
			ImGui.EndTabBar();
		}
		
		private void OpenMod(Mod mod) {
			mod.LoadPreviews();
			newestMod = mod.ID;
			if(!modsOpen.Contains(mod))
				modsOpen.Add(mod);
		}
		
		private void DrawModPage(Mod mod) {
			ImGui.BeginChild("AethermentModPage", new Vector2(0, 300 * ImGuiHelpers.GlobalScale + ImGuiAeth.SpacingY * 2), false, ImGuiWindowFlags.HorizontalScrollbar);
			// previews
			for(int i = 0; i < mod.Previews.Count; i++) {
				var preview = mod.Previews[i];
				
				if(i > 0)
					ImGui.SameLine();
				ImGuiAeth.Image(preview, ImGui.GetCursorScreenPos(), new Vector2(preview.Width, preview.Height) * (300f / preview.Height));
				// ImGui.Image(preview.ImGuiHandle, new Vector2(preview.Width, preview.Height) * (300f / preview.Height));
			}
			
			// todo: the rest
			
			ImGui.EndChild();
		}
	}
}