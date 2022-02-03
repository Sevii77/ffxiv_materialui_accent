using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Logging;
using Dalamud.Interface;

using Aetherment.Util;

namespace Aetherment.GUI {
	internal partial class UI {
		private string search = "";
		private bool adv = true;
		private Dictionary<string, bool> advtags;
		private List<Mod> drawmods;
		
		private void DrawMods() {
			float h = ImGuiAeth.Height();
			uint w = (uint)ImGuiAeth.WidthLeft(h * 1.5f);
			ImGui.SetNextItemWidth(w);
			if(ImGui.InputTextWithHint("", "Search", ref search, w))
				SearchMods();
			ImGui.SameLine();
			if(ImGuiAeth.ButtonIcon(FontAwesomeIcon.Cog, new Vector2(h * 1.5f, h)))
				adv = !adv;
			ImGuiAeth.HoverTooltip("Advanced search options");
			
			if(adv) {
				w = 125;
				float tw = ImGuiAeth.WidthLeft();
				int c = Math.Max(1, ImGuiAeth.PossibleCount(w, tw));
				
				foreach(Dictionary<string, string> categorized in Mod.TagNames) {
					int i = 0;;
					
					foreach(KeyValuePair<string, string> tag in categorized) {
						string id = tag.Key;
						string name = tag.Value;
						
						ImGui.BeginChild(id, new Vector2(w, h));
						bool tmp = advtags[id];
						if(ImGui.Checkbox(name, ref tmp))
							SearchMods();
						advtags[id] = tmp;
						ImGui.EndChild();
						
						if(i < categorized.Count - 1 && i % c != c - 1)
							ImGui.SameLine();
						i++;
					}
					
					ImGuiAeth.Offset(0, 10);
				}
			}
			
			foreach(Mod mod in drawmods) {
				DrawModSimple(mod);
			}
		}
		
		private void SearchMods() {
			Task.Run(async() => {
				List<string> tags = new();
				foreach(KeyValuePair<string, bool> tag in advtags)
					if(tag.Value)
						tags.Add(tag.Key);
				
				drawmods = await Mod.GetMods(Aetherment.repos, search, tags);
			});
		}
		
		private void DrawMod(Mod mod) {
			
		}
		
		private void DrawModSimple(Mod mod) {
			ImGui.Text(mod.Name);
		}
		
		private void DrawModPage(Mod mod) {
			
		}
	}
}