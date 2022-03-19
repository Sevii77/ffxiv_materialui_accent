using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

using ImGuiNET;
using ImGuiScene;
using Dalamud.Interface;

using Lumina.Data.Files;

namespace Aetherment.GUI.Explorer {
	internal class Tex : Viewer {
		private static bool enableA = true;
		private static bool enableR = true;
		private static bool enableG = true;
		private static bool enableB = true;
		private static bool details = true;
		private static int depthLevel = 0;
		private static int mipLevel = 0;
		
		private TexFile file;
		private TextureWrap preview;
		
		public Tex(ulong hash, string path) : base(hash, path) {
			var ext = "." + path.Split(".").Last();
			importValids = new(){ext, ".dds"};
			exportValids = new(){ext, ".dds"};
			
			try {
				file = GetFile<TexFile>(hash, path);
				LoadPreview();
			} catch(Exception e) {
				ShowError(e.ToString());
			}
		}
		
		public override void Dispose() {
			if(preview != null)
				preview.Dispose();
		}
		
		public override byte[] GetFileData() {
			return file.Data;
		}
		
		protected override void DrawViewer() {
			var head = file.Header;
			
			var changed = false;
			changed = ImGui.Checkbox("R", ref enableR) || changed;
			ImGui.SameLine();
			changed = ImGui.Checkbox("G", ref enableG) || changed;
			ImGui.SameLine();
			changed = ImGui.Checkbox("B", ref enableB) || changed;
			ImGui.SameLine();
			changed = ImGui.Checkbox("A", ref enableA) || changed;
			
			ImGui.SameLine();
			var w = ImGuiAeth.WidthLeft(ImGuiAeth.Height() + ImGui.CalcTextSize("Details").X + ImGuiAeth.ISpacingX);
			// w -= ImGui.CalcTextSize("MipMap").X + ImGui.CalcTextSize("Depth").X + ImGuiAeth.ISpacingX * 2 + ImGuiAeth.SpacingX;
			
			if(head.MipLevels > 1)
				w -= ImGui.CalcTextSize("MipMap").X + ImGuiAeth.ISpacingX;
			if(head.Depth > 1)
				w -= ImGui.CalcTextSize("Depth").X + ImGuiAeth.ISpacingX;
			if(head.MipLevels > 1 && head.Depth > 1)
				w = (w - ImGuiAeth.SpacingX) / 2;
			
			if(head.MipLevels > 1) {
				ImGui.SetNextItemWidth(w);
				changed = ImGui.SliderInt("MipMap", ref mipLevel, 0, head.MipLevels - 1) || changed;
			}
			
			if(head.Depth > 1) {
				ImGui.SameLine();
				ImGui.SetNextItemWidth(w);
				changed = ImGui.SliderInt("Depth", ref depthLevel, 0, (int)Math.Ceiling(head.Depth / Math.Pow(2, mipLevel)) - 1) || changed;
			} else if(head.MipLevels <= 1)
				ImGui.Dummy(new Vector2(w, 0));
			
			ImGui.SameLine();
			ImGui.Checkbox("Details", ref details);
			
			if(changed)
				LoadPreview();
			
			var pos = ImGui.GetCursorPos();
			if(preview != null)
				ImGuiAeth.Image(preview, ImGui.GetCursorScreenPos(), ImGui.GetContentRegionAvail() / ImGuiHelpers.GlobalScale);
			else {
				ImGuiAeth.Offset(ImGui.GetContentRegionAvail() / 2 - ImGui.CalcTextSize("Failed to load preview") / 2, false);
				ImGui.Text("Failed to load preview");
			}
			
			if(details) {
				ImGui.SetCursorPos(pos);
				ImGui.Text($"Type: {head.Type}");
				ImGui.Text($"Format: {head.Format}");
				ImGui.Text($"Width: {head.Width}");
				ImGui.Text($"Height: {head.Height}");
				ImGui.Text($"Depth: {head.Depth}");
				ImGui.Text($"MipMap Count: {head.MipLevels}");
				unsafe {
					ImGui.Text($"Lods:");
					for(int i = 0; i < 3; i++)
						ImGui.Text($"{i}: {head.LodOffset[i]}");
					ImGui.Text($"MipMap Offsets:");
					for(int i = 0; i < 13; i++)
						ImGui.Text($"{i}: {head.OffsetToSurface[i]}");
				}
			}
		}
		
		private void LoadPreview() {
			if(preview != null)
				preview.Dispose();
			
			try {
				byte[] org;
				
				var width = (int)file.Header.Width;
				var height = (int)file.Header.Height;
				var xyScalar = Math.Pow(2, mipLevel);
				mipLevel = Math.Clamp(mipLevel, 0, file.Header.MipLevels - 1);
				depthLevel = Math.Clamp(depthLevel, 0, (int)Math.Ceiling(file.Header.Depth / (float)xyScalar) - 1);
				
				if(mipLevel == 0 && depthLevel == 0)
					org = file.ImageData;
				else {
					width = (int)(file.Header.Width / xyScalar);
					height = (int)(file.Header.Height / xyScalar);
					var planeSize = 0;
					var offset = 80;
					unsafe {
						// planeSize = (int)(((file.Header.MipLevels > 1 ? file.Header.OffsetToSurface[1] : file.Data.Length) - 80) / (xyScalar * xyScalar * (file.Header.Depth / xyScalar)));
						planeSize = (int)(((mipLevel < file.Header.MipLevels - 1 ? file.Header.OffsetToSurface[mipLevel + 1] : file.Data.Length) - file.Header.OffsetToSurface[mipLevel]) / (file.Header.Depth / xyScalar));
						offset = (int)file.Header.OffsetToSurface[mipLevel] + planeSize * depthLevel;
					}
					
					org = file.Convert(new Span<byte>(file.Data).Slice(offset), width, height);
				}
				
				var data = new byte[org.Length];
				var offclr = (byte)((!enableR && !enableG && !enableB) ? 255 : 0);
				for(int i = 0; i <= org.Length - 4; i += 4) {
					data[i    ] = enableR ? org[i + 2] : offclr;
					data[i + 1] = enableG ? org[i + 1] : offclr;
					data[i + 2] = enableB ? org[i    ] : offclr;
					data[i + 3] = enableA ? org[i + 3] : (byte)255;
				}
				
				preview = Aetherment.Interface.UiBuilder.LoadImageRaw(data, width, height, 4);
			} catch(Exception e) {
				ShowError(e.ToString());
			}
		}
	}
}