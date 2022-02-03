using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Logging;

using Aetherment.Util;

namespace Aetherment.GUI {
	internal partial class UI : IDisposable {
		private bool shouldDraw = false;
		
		public UI() {
			advtags = new();
			foreach(Dictionary<string, string> categorized in Mod.TagNames)
				foreach(KeyValuePair<string, string> tag in categorized)
					advtags[tag.Key] = false;
			
			drawmods = new();
			Task.Run(async() => {
				drawmods = await Mod.GetMods(Aetherment.repos);
			});
			
			Show();
			
			Aetherment.Interface.UiBuilder.OpenConfigUi += Show;
			Aetherment.Interface.UiBuilder.Draw += Draw;
		}
		
		public void Dispose() {
			Aetherment.Interface.UiBuilder.OpenConfigUi -= Show;
			Aetherment.Interface.UiBuilder.Draw -= Draw;
		}
		
		public void Show() {
			shouldDraw = true;
		}
		
		private void Draw() {
			if(!shouldDraw)
				return;
			
			ImGui.SetNextWindowSize(new Vector2(720, 480));
			ImGui.Begin("Aetherment", ref shouldDraw);
			// DrawTest();
			ImGui.BeginTabBar("Aetherment");
			if(ImGui.BeginTabItem("Settings")) {
				DrawSettings();
				ImGui.EndTabItem();
			}
			
			if(ImGui.BeginTabItem("Configuration")) {
				DrawConfig();
				ImGui.EndTabItem();
			}
			
			if(ImGui.BeginTabItem("Mod Browser")) {
				DrawMods();
				ImGui.EndTabItem();
			}
			
			if(ImGui.BeginTabItem("Test")) {
				DrawTest();
				ImGui.EndTabItem();
			}
			
			ImGui.End();
		}
		
		private void DrawTest() {
			float x = ImGui.GetStyle().ItemSpacing.X;
			
			// Filling test
			ImGui.Button("left", new Vector2(100, 40));
			ImGui.SameLine();
			float w = ImGuiAeth.WidthLeft(100, 2);
			ImGui.Button("center", new Vector2(w, 40));
			ImGui.SameLine();
			ImGui.Button("center", new Vector2(w, 40));
			ImGui.SameLine();
			ImGui.Button("right", new Vector2(100, 40));
			
			ImGui.Button("left", new Vector2(100, 40));
			ImGui.SameLine();
			w = ImGuiAeth.WidthLeft(new float[2] {50, 100}, 3);
			ImGui.Button("center", new Vector2(w, 40));
			ImGui.SameLine();
			ImGui.Button("center", new Vector2(w, 40));
			ImGui.SameLine();
			ImGui.Button("center", new Vector2(w, 40));
			ImGui.SameLine();
			ImGui.Button("right", new Vector2(50, 40));
			ImGui.SameLine();
			ImGui.Button("right", new Vector2(100, 40));
			
			ImGui.Button("left", new Vector2(100, 40));
			ImGui.SameLine();
			w = ImGuiAeth.WidthLeft(new float[2] {150, 100});
			ImGui.Button("center", new Vector2(w, 40));
			ImGui.SameLine();
			ImGui.Button("right", new Vector2(150, 40));
			ImGui.SameLine();
			ImGui.Button("right", new Vector2(100, 40));
			
			// Grid test
			w = 150;
			float tw = ImGuiAeth.WidthLeft();
			int c = Math.Max(1, ImGuiAeth.PossibleCount(w, tw));
			float o = (tw - ((x + w) * c - x)) / 2 + x;
			for(int i = 0; i < 20; i++) {
				if(i % c == 0)
					ImGui.SetCursorPosX(o);
				
				ImGui.BeginChild("" + i, new Vector2(w, 200));
				ImGui.TextWrapped("It sportsman earnestly ye preserved an on. Moment led family sooner cannot her window pulled any. Or raillery if improved landlord to speaking hastened differed he. Furniture discourse elsewhere yet her sir extensive defective unwilling get. Why resolution one motionless you him thoroughly. Noise is round to in it quick timed doors. Written address greatly get attacks inhabit pursuit our but. Lasted hunted enough an up seeing in lively letter. Had judgment out opinions property the supplied.");
				ImGui.EndChild();
				
				if(i < 19 && i % c != c - 1)
					ImGui.SameLine();
			}
		}
	}
}