using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Interface;

using Aetherment.Util;

namespace Aetherment.GUI {
	internal partial class UI {
		internal class ModPage {
			public Mod Mod;
			private int page = 0;
			
			public ModPage(Mod mod) {
				Mod = mod;
			}
			
			public void Draw() {
				ImGui.BeginChildFrame(Mod.ID2, new Vector2(250 * ImGuiHelpers.GlobalScale + ImGuiAeth.SpacingX * 2, 0));
				// ImGui.BeginChild("AethermentModPage", new Vector2(-1, ImGuiAeth.HeightLeft() - ImGuiAeth.Height() - ImGuiAeth.PaddingY - ImGuiAeth.PaddingX));
				
				// Name
				ImGuiAeth.Offset(0, 5);
				ImGui.SetWindowFontScale(1.2f);
				ImGuiAeth.TextCentered(Mod.Name, ImGuiAeth.WidthLeft());
				ImGui.SetWindowFontScale(1f);
				
				// Author
				ImGuiAeth.Offset((ImGuiAeth.WidthLeft() - ImGui.CalcTextSize("by " + Mod.Author).X) / 2, -ImGuiAeth.SpacingY, false);
				ImGui.TextDisabled("by ");
				ImGui.SameLine();
				ImGuiAeth.Offset(-ImGuiAeth.SpacingX, 0, false);
				ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[18]);
				ImGui.Text(Mod.Author);
				ImGui.PopStyleColor();
				ImGuiAeth.HoverTooltip(Mod.AuthorContact);
				
				ImGuiAeth.TextCentered(string.Join("   ", Mod.TagsFancy), ImGuiAeth.WidthLeft());
				
				ImGuiAeth.Offset(0, 10);
				ImGui.TextWrapped(Mod.Description);
				
				// Links
				var h = ImGuiAeth.Height();
				ImGuiAeth.Offset(0, ImGuiAeth.HeightLeft() - ImGuiAeth.Height() + ImGuiAeth.PaddingY - ImGuiAeth.PaddingX, false);
				for(int i = 0; i < Mod.Links.Length; i++) {
					ImGuiAeth.ButtonSocial(new Vector2(h), Mod.Links[i]);
					ImGui.SameLine();
				}
				
				ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
				if(ImGui.Button($"Download  ({Math.Round(Mod.Size / (float)(1024 * 1024) * 10) / 10} MB)", new Vector2(ImGuiAeth.WidthLeft(), h)))
					Installer.DownloadMod(Mod);
				ImGui.PopStyleVar();
				
				ImGui.EndChildFrame();
				
				// Previews
				ImGui.SameLine();
				ImGui.BeginChild("previews", Vector2.Zero);
				var pos = ImGui.GetCursorPos();
				
				h = ImGuiAeth.Height();
				var height = ImGuiAeth.HeightLeft() - h;
				ImGuiAeth.Offset(ImGuiAeth.XOffset(ImGuiAeth.WidthLeft(), Mod.Previews.Count, h), height, false);
				
				for(int i = 0; i < Mod.Previews.Count; i++) {
					if(i > 0)
						ImGui.SameLine();
					ImGui.PushID(i + 100);
					ImGui.RadioButton("", ref page, i);
					ImGui.PopID();
				}
				
				ImGui.SetCursorPos(pos);
				
				for(int i = 0; i < Mod.Previews.Count; i++)
					if(page == i)
						// ImGuiAeth.Image(Mod.Previews[i], new Vector2(ImGuiAeth.WidthLeft(), height));
						ImGuiAeth.Image(Mod.Previews[i], ImGui.GetCursorScreenPos(), new Vector2(ImGuiAeth.WidthLeft(), height - ImGuiAeth.SpacingY) / ImGuiHelpers.GlobalScale);
				
				ImGui.EndChild();
			}
		}
		
		private List<ModPage> modsOpen;
		private string newestMod;
		
		private void DrawMods() {
			ImGui.BeginTabBar("AethermentMods");
			
			List<ModPage> newOpen = new();
			foreach(var mod in modsOpen) {
				var open = true;
				if(ImGui.BeginTabItem(mod.Mod.Name, ref open, newestMod == mod.Mod.ID ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None)) {
					ImGui.BeginChild("AethermentTab", ImGui.GetContentRegionAvail());
					mod.Draw();
					ImGui.EndChild();
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
			if(!modsOpen.Exists(x => x.Mod == mod))
				modsOpen.Add(new(mod));
		}
	}
}