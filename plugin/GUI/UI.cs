using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Interface;

using Aetherment.Util;

namespace Aetherment.GUI {
	internal partial class UI : IDisposable {
		private bool shouldDraw = false;
		private TitleScreenMenu.TitleScreenMenuEntry titleMenu;
		private Explorer.Explorer explorer;
		
		public UI() {
			advtags = new();
			foreach(Dictionary<string, string> categorized in Mod.TagNames)
				foreach(KeyValuePair<string, string> tag in categorized)
					advtags[tag.Key] = false;
			
			drawmods = new();
			Task.Run(async() => {
				drawmods = await Mod.GetMods();
				// drawmods = await Mod.GetMods(Aetherment.Config.Repos);
				
				foreach(var mod in Aetherment.Config.InstalledMods)
					AddLocalMod(mod);
			});
			
			modsOpen = new();
			explorer = new();
			
			Show();
			
			Aetherment.Interface.UiBuilder.OpenConfigUi += Show;
			Aetherment.Interface.UiBuilder.Draw += Draw;
			
			titleMenu = Aetherment.TitleMenu.AddEntry("Aetherment", Aetherment.Textures["icon64.png"], Show);
		}
		
		public void Dispose() {
			foreach(Mod mod in drawmods)
				mod.Dispose();
			
			explorer.Dispose();
			
			Aetherment.Interface.UiBuilder.OpenConfigUi -= Show;
			Aetherment.Interface.UiBuilder.Draw -= Draw;
			
			if(titleMenu != null)
				Aetherment.TitleMenu.RemoveEntry(titleMenu);
		}
		
		public void Show() {
			shouldDraw = !shouldDraw;
		}
		
		private void Draw() {
			if(!shouldDraw)
				return;
			
			// Installer.InstallStatus.Busy = true;
			// Installer.InstallStatus.Progress = 6;
			// Installer.InstallStatus.Total = 10;
			// Installer.InstallStatus.CurrentJob = "Testing ABC (yay)";
			
			var footer = (Installer.InstallStatus.Busy ? (ImGuiAeth.Height() + ImGuiAeth.SpacingY) : 0);
			var bodySize = new Vector2(0, -footer);
			
			// ImGui.SetNextWindowSize(new Vector2(1070, 600));
			ImGui.SetNextWindowSize(new Vector2(1070, 600), ImGuiCond.FirstUseEver);
			ImGui.Begin("Aetherment", ref shouldDraw);
			// DrawTest();
			ImGui.BeginTabBar("Aetherment");
			if(ImGui.BeginTabItem("Settings")) {
				ImGui.BeginChild("Settings", bodySize);
				DrawSettings();
				ImGui.EndChild();
				ImGui.EndTabItem();
			}
			
			if(ImGui.BeginTabItem("Configuration")) {
				ImGui.BeginChild("Configuration", bodySize);
				DrawConfig();
				ImGui.EndChild();
				ImGui.EndTabItem();
			}
			
			if(ImGui.BeginTabItem("Mod Browser")) {
				ImGui.BeginChild("Browser", bodySize);
				DrawModBrowser();
				ImGui.EndChild();
				ImGui.EndTabItem();
			}
			
			if(modsOpen.Count > 0)
				if(ImGuiAeth.BeginTabItem("Mods", newestMod != null ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None)) {
					ImGui.BeginChild("Mods", bodySize);
					DrawMods();
					ImGui.EndChild();
					ImGui.EndTabItem();
				}
			
			if(Aetherment.Config.DevMode)
				if(ImGui.BeginTabItem("File Explorer")) {
					ImGui.BeginChild("Explorer", bodySize);
					explorer.Draw();
					ImGui.EndChild();
					ImGui.EndTabItem();
				}
			ImGui.EndTabBar();
			
			// Installer progress bar
			if(Installer.InstallStatus.Busy) {
				var pos = ImGui.GetCursorScreenPos();
				var size = new Vector2(ImGuiAeth.WidthLeft(), ImGuiAeth.Height());
				var progress = (float)Installer.InstallStatus.Progress / Installer.InstallStatus.Total;
				
				ImGui.ProgressBar(progress, size, "");
				
				var postext = pos + size / 2 - new Vector2(0, ImGuiAeth.Height() * 0.25f) - ImGui.CalcTextSize(Installer.InstallStatus.CurrentJob) / 2;
				var postext2 = pos + size / 2 + new Vector2(0, ImGuiAeth.Height() * 0.25f) - ImGui.CalcTextSize(Installer.InstallStatus.CurrentJobDetails) * (14 / ImGui.GetFont().FontSize) / 2;
				ImGui.PushClipRect(pos, pos + new Vector2(size.X * progress, size.Y), true);
				// 7 = FrameBg
				ImGui.GetWindowDrawList().AddText(postext, ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[7]), Installer.InstallStatus.CurrentJob);
				ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), 14, postext2, ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[7]), Installer.InstallStatus.CurrentJobDetails);
				ImGui.PopClipRect();
				
				ImGui.PushClipRect(pos + new Vector2(size.X * progress, 0), pos + size, true);
				// 42 = PlotHistogram
				ImGui.GetWindowDrawList().AddText(postext, ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[42]), Installer.InstallStatus.CurrentJob);
				ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), 14, postext2, ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[42]), Installer.InstallStatus.CurrentJobDetails);
				ImGui.PopClipRect();
			}
			
			ImGui.End();
		}
	}
}