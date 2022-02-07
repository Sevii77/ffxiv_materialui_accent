using System;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using ImGuiNET;
using ImGuiScene;
using Dalamud.Interface;
using Dalamud.Logging;

namespace Aetherment.GUI {
	public class ImGuiAeth {
		public static Vector2 Spacing => ImGui.GetStyle().ItemSpacing;
		public static float SpacingX => ImGui.GetStyle().ItemSpacing.X;
		public static float SpacingY => ImGui.GetStyle().ItemSpacing.Y;
		public static Vector2 Padding => ImGui.GetStyle().FramePadding;
		public static float PaddingX => ImGui.GetStyle().FramePadding.X;
		public static float PaddingY => ImGui.GetStyle().FramePadding.Y;
		
		private static int gridCount;
		private static int gridIndex;
		private static Vector2 gridItemPos;
		private static Vector2 gridItemSize;
		
		public static float WidthLeft() {
			return ImGui.GetColumnWidth();
		}
		
		public static float WidthLeft(float after, int splitCount = 1) {
			return (ImGui.GetColumnWidth() - after - SpacingX * splitCount) / splitCount;
		}
		
		public static float WidthLeft(float[] after, int splitCount = 1) {
			return ((ImGui.GetColumnWidth() - after.Sum() - SpacingX * after.Length) - SpacingX * (splitCount - 1)) / splitCount;
		}
		
		public static float Height() {
			return PaddingY * 2 + ImGui.GetFontSize();
		}
		
		public static void Offset(float x, float y, bool globalScale = true) {
			if(globalScale) {
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + x * ImGuiHelpers.GlobalScale);
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + y * ImGuiHelpers.GlobalScale);
			} else {
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + x);
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + y);
			}
		}
		
		public static void HoverTooltip(string label) {
			if(ImGui.IsItemHovered())
				ImGui.SetTooltip(label);
		}
		
		public static bool ButtonIcon(FontAwesomeIcon icon, Vector2 size) {
			ImGui.PushFont(UiBuilder.IconFont);
			
			var pos = ImGui.GetCursorPos();
			bool hover = false;
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, PaddingY));
			ImGui.Dummy(size);
			if(ImGui.IsItemHovered()) {
				ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[1]);
				hover = true;
			}
			
			ImGui.SetCursorPos(pos);
			bool a = ImGui.Button(icon.ToIconString(), size);
			ImGui.PopStyleVar();
			
			if(hover)
				ImGui.PopStyleColor();
			ImGui.PopFont();
			
			return a;
		}
		
		public static void Image(TextureWrap tex, Vector2 bounds, bool center = true) {
			float scale = Math.Min(bounds.X / tex.Width, bounds.Y / tex.Height);
			float x = tex.Width * scale;
			float y = tex.Height * scale;
			
			if(center)
				ImGuiAeth.Offset((bounds.X - x) / 2, (bounds.Y - y) / 2, false);
			
			ImGui.Image(tex.ImGuiHandle, new Vector2(x, y));
		}
		
		public static void Image(TextureWrap tex, Vector2 pos, Vector2 size) {
			size *= ImGuiHelpers.GlobalScale;
			
			var rounding = (uint)ImGui.GetStyle().FrameRounding;
			ImGui.Dummy(size);
			ImGui.GetWindowDrawList().AddRectFilled(pos, pos + size, 0xFF101010, rounding);
			
			if(tex != null) {
				// doing the corners on the image like this kinda sucks
				// TODO: figure out how to use render masks, might have to use raw rendering methods
				var scale = Math.Min(size.X / tex.Width, size.Y / tex.Height);
				var w = tex.Width * scale;
				var h = tex.Height * scale;
				pos.X += (size.X - w) / 2;
				pos.Y += (size.Y - h) / 2;
				ImGui.GetWindowDrawList().AddImageRounded(tex.ImGuiHandle, pos, pos + new Vector2(w, h), Vector2.Zero, Vector2.One, 0xFFFFFFFF, rounding - Math.Min(rounding, Math.Max(size.X - w, size.Y - h) / 2));
			}
		}
		
		public static void TextBounded(string text, Vector2 size) {
			float height = 0;
			string section = "";
			string[] segments = text.Split(' ');
			string final = "";
			float addLength = ImGui.CalcTextSize("...").X;
			
			foreach(string segment in segments) {
				var s = ImGui.CalcTextSize(section + (section == "" ? "" : " ") + segment);
				
				if(s.X + (height + s.Y * 2 > size.Y ? addLength : 0) > size.X) {
					final += section;
					section = segment;
					height += s.Y;
					
					if(height + s.Y > size.Y) {
						section = "";
						final += "...";
						break;
					} else
						final += "\n";
				} else {
					section += (section == "" ? "" : " ") + segment;
				}
			}
			
			ImGui.Text(final + section);
		}
		
		// why tf do i need this, why doesnt a varient exist without the open bool ref
		public static unsafe bool BeginTabItem(string label, ImGuiTabItemFlags flags) {
			var len = Encoding.UTF8.GetByteCount(label);
			var ptr = stackalloc byte[len + 1];
			fixed(char* chars = label)
				ptr[Encoding.UTF8.GetBytes(chars, label.Length, ptr, len)] = 0;
			return ImGuiNative.igBeginTabItem(ptr, (byte*)null, flags) != 0;
		}
		
		public static void BeginGrid(float maxWidth, Vector2 itemSize) {
			gridCount = (int)Math.Floor(maxWidth / (itemSize.X + ImGuiAeth.PaddingX) + ImGuiAeth.PaddingX / (itemSize.X + ImGuiAeth.PaddingX));
			gridIndex = 0;
			gridItemPos = ImGui.GetCursorPos();
			gridItemSize = itemSize;
		}
		
		public static void NextGridItem() {
			ImGui.SetCursorPos(gridItemPos);
			
			gridIndex++;
			if(gridCount == 0 || gridIndex % gridCount == 0)
				gridItemPos += new Vector2(-(gridItemSize.X + ImGuiAeth.PaddingX) * Math.Max(0, gridCount - 1), gridItemSize.Y + ImGuiAeth.PaddingX);
			else
				gridItemPos.X += gridItemSize.X + ImGuiAeth.PaddingX;
			
		}
	}
}