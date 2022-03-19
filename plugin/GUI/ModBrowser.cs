using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Interface;

using Aetherment.Util;

namespace Aetherment.GUI {
	internal partial class UI {
		private string search = "";
		private bool adv = false;
		private bool gridDisplay = false;
		private Dictionary<string, bool> advtags;
		private List<Mod> drawmods;
		
		private void DrawModBrowser() {
			var h = ImGuiAeth.Height();
			var w = (uint)ImGuiAeth.WidthLeft(new float[]{h * 1f, h * 1f});
			
			// functionality of clear button here because else it wont work
			ImGuiAeth.Offset(w - h, 0, false);
			ImGui.Dummy(new Vector2(h));
			if(ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
				search = "";
				SearchMods();
			}
			
			ImGui.SameLine();
			ImGuiAeth.Offset(-w - ImGuiAeth.SpacingX, 0, false);
			ImGui.SetNextItemWidth(w);
			if(ImGui.InputTextWithHint("", "Search", ref search, w))
				SearchMods();
				
			ImGui.SameLine();
			ImGuiAeth.Offset(-h - ImGuiAeth.SpacingX, 0, false);
			ImGuiAeth.ButtonIcon(FontAwesomeIcon.Times);
			ImGuiAeth.HoverTooltip("Clear");
				
			ImGui.SameLine();
			if(ImGuiAeth.ButtonIcon(FontAwesomeIcon.Cog))
				adv = !adv;
			ImGuiAeth.HoverTooltip("Advanced search options");
			
			ImGui.SameLine();
			if (ImGuiAeth.ButtonIcon(gridDisplay ? FontAwesomeIcon.ThLarge : FontAwesomeIcon.ThList))
				gridDisplay = !gridDisplay;
			
			if(adv) {
				foreach(Dictionary<string, string> categorized in Mod.TagNames) {
					ImGuiAeth.BeginGrid(ImGuiAeth.WidthLeft(), new Vector2(125 * ImGuiHelpers.GlobalScale, ImGuiAeth.Height()));
					foreach(KeyValuePair<string, string> tag in categorized) {
						string id = tag.Key;
						
						ImGuiAeth.NextGridItem();
						bool tmp = advtags[id];
						if(ImGui.Checkbox(tag.Value, ref tmp))
							SearchMods();
						advtags[id] = tmp;
					}
					
					ImGuiAeth.Offset(0, 10);
				}
			}
			
			ImGui.BeginChild("AethermentModBrowser");
			if(gridDisplay) {
				ImGuiAeth.BeginGrid(ImGuiAeth.WidthLeft(), new Vector2(200 * ImGuiHelpers.GlobalScale));
				foreach(Mod mod in drawmods) {
					ImGuiAeth.NextGridItem();
					DrawModPreviewGrid(mod);
				}
			} else
				foreach(Mod mod in drawmods)
					DrawModPreviewList(mod);
			ImGui.EndChild();
		}
		
		private void SearchMods() {
			Task.Run(async() => {
				List<string> tags = new();
				foreach(KeyValuePair<string, bool> tag in advtags)
					if(tag.Value)
						tags.Add(tag.Key);
				
				drawmods = await Mod.GetMods(search, tags);
				// drawmods = await Mod.GetMods(Aetherment.Config.Repos, search, tags);
			});
		}
		
		private void DrawModPreviewGrid(Mod mod) {
			ImGui.BeginChildFrame(mod.ID2, new Vector2(200 * ImGuiHelpers.GlobalScale), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
			// pop the clip rect from the child frame since ONLY the x is affected by FramePadding (why only X? why is it even affected in general? wtf)
			ImGui.PopClipRect();
			
			var pos = ImGui.GetCursorScreenPos() + new Vector2(0, 100 * ImGuiHelpers.GlobalScale) - ImGuiAeth.Padding + new Vector2(1, 0);
			
			// info
			ImGui.BeginGroup();
				// DrawInstalled(mod);
				ImGui.Text(mod.Name);
				
				if(Aetherment.Config.InstalledMods.Contains(mod.ID)) {
					ImGui.SameLine();
					ImGuiAeth.Offset(ImGuiAeth.WidthLeft() - ImGuiAeth.Height(), 0, false);
					DrawInstalled();
				}
				
				ImGuiAeth.Offset(10, -ImGuiAeth.SpacingY);
				var suppress = DrawAuthor(mod);
				
				// ImGui.TextWrapped(mod.Description);
				ImGuiAeth.TextBounded(mod.Description, new Vector2(ImGuiAeth.WidthLeft(), 100 * ImGuiHelpers.GlobalScale - ImGuiAeth.Height() * 2 - ImGuiAeth.Spacing.Y));
			ImGui.EndGroup();
			
			ImGuiAeth.Image(mod.Previews.Count > 0 ? mod.Previews[0] : null, pos, new Vector2(198, 99));
			
			// push a new clip rect since it will expect one from child frame and pop it
			ImGui.PushClipRect(Vector2.Zero, Vector2.Zero, true);
			// open mod page
			ImGui.EndChildFrame();
			if(ImGui.IsItemClicked() && !suppress)
				OpenMod(mod);
		}
		
		private void DrawModPreviewList(Mod mod) {
			ImGui.BeginChildFrame(mod.ID2, new Vector2(ImGuiAeth.WidthLeft(), 100 * ImGuiHelpers.GlobalScale), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
			ImGui.PopClipRect();
			
			ImGuiAeth.Image(mod.Previews.Count > 0 ? mod.Previews[0] : null, ImGui.GetCursorScreenPos() - ImGuiAeth.Padding + Vector2.One, new Vector2(196, 98));
			
			// info
			ImGui.SameLine();
			ImGui.BeginGroup();
				// DrawInstalled(mod);
				
				ImGuiAeth.Offset(-ImGuiAeth.PaddingX, 0);
				ImGui.Text(mod.Name);
				
				ImGui.SameLine();
				ImGuiAeth.Offset(10 - ImGuiAeth.SpacingX - ImGuiAeth.PaddingX, 0, false);
				var suppress = DrawAuthor(mod);
				
				if(Aetherment.Config.InstalledMods.Contains(mod.ID)) {
					ImGui.SameLine();
					ImGuiAeth.Offset(ImGuiAeth.WidthLeft() - ImGuiAeth.Height(), 0, false);
					DrawInstalled();
				}
				
				ImGuiAeth.Offset(-ImGuiAeth.PaddingX, -ImGuiAeth.SpacingY);
				ImGui.TextDisabled(string.Join("   ", mod.TagsFancy));
				
				ImGuiAeth.Offset(-ImGuiAeth.PaddingX, 0);
				ImGuiAeth.TextBounded(mod.Description, new Vector2(ImGuiAeth.WidthLeft(), 100 * ImGuiHelpers.GlobalScale - ImGuiAeth.Height() * 2 - ImGuiAeth.Spacing.Y));
			ImGui.EndGroup();
			
			ImGui.PushClipRect(Vector2.Zero, Vector2.Zero, true);
			// open mod page
			ImGui.EndChildFrame();
			if(ImGui.IsItemClicked() && !suppress)
				OpenMod(mod);
		}
		
		private void DrawInstalled() {
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.SetWindowFontScale(0.8f);
			ImGuiAeth.Offset(5, 3);
			ImGui.Text(FontAwesomeIcon.Check.ToIconString());
			ImGuiAeth.Offset(0, -3);
			ImGui.SetWindowFontScale(1f);
			ImGui.PopFont();
			ImGuiAeth.HoverTooltip("Installed");
		}
		
		private bool DrawAuthor(Mod mod) {
			var clicked = false;
			
			ImGui.TextDisabled("by ");
			ImGui.SameLine();
			ImGuiAeth.Offset(-ImGuiAeth.SpacingX, 0, false);
			ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[18]);
			ImGui.Text(mod.Author);
			ImGui.PopStyleColor();
			if(ImGui.IsItemClicked()) {
				clicked = true;
				search = mod.Author;
				SearchMods();
			}
			ImGuiAeth.HoverTooltip(mod.AuthorContact);
			
			return clicked;
		}
	}
}