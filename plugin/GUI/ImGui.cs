using System;
using System.Linq;
using System.Numerics;

using ImGuiNET;
using Dalamud.Interface;

namespace Aetherment.GUI {
	public class ImGuiAeth {
		public static float WidthLeft() {
			return ImGui.GetColumnWidth();
		}
		
		public static float WidthLeft(float after, int splitCount = 1) {
			return (ImGui.GetColumnWidth() - after - ImGui.GetStyle().ItemSpacing.X * splitCount) / splitCount;
		}
		
		public static float WidthLeft(float[] after, int splitCount = 1) {
			float x = ImGui.GetStyle().ItemSpacing.X;
			return ((ImGui.GetColumnWidth() - after.Sum() - x * after.Length) - x * (splitCount - 1)) / splitCount;
		}
		
		public static float Height() {
			return ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetFontSize();
		}
		
		public static int PossibleCount(float frameWidth, float space) {
			float x = ImGui.GetStyle().ItemSpacing.X;
			frameWidth += x;
			
			return (int)Math.Floor(space / frameWidth + x / frameWidth);
		}
		
		public static void HoverTooltip(string label) {
			if(ImGui.IsItemHovered())
				ImGui.SetTooltip(label);
		}
		
		public static bool ButtonIcon(FontAwesomeIcon icon, Vector2 size) {
			ImGui.PushFont(UiBuilder.IconFont);
			bool a = ImGui.Button(icon.ToIconString(), size);
			ImGui.PopFont();
			
			return a;
		}
		
		public static bool ButtonIcon(FontAwesomeIcon icon) {
			ImGui.PushFont(UiBuilder.IconFont);
			bool a = ImGui.Button(icon.ToIconString());
			ImGui.PopFont();
			
			return a;
		}
		
		public static void Offset(float x, float y) {
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + x);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + y);
		}
	}
}