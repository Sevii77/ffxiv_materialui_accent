using System;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Interface;

using Lumina.Data;

namespace Aetherment.GUI.Explorer {
	internal class Raw : Viewer {
		private static readonly byte[] binaryChars = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 127};
		
		private FileResource file;
		private string[] lines;
		private int displayType = 0;
		
		public Raw(ulong hash, string path) : base(hash, path) {
			var ext = "." + (path.Contains('.') ? path.Split(".").Last() : ".txt");
			importValids = new(){ext};
			exportValids = new(){ext};
			
			try {
				file = GetFile<FileResource>(hash, path);
				
				var isBinary = false;
				foreach(var v in file.Data)
					if(Array.IndexOf(binaryChars, v) != -1) {
						isBinary = true;
						break;
					}
				GetLines(isBinary);
			} catch(Exception e) {
				ShowError(e.ToString());
			}
		}
		
		public override byte[] GetFileData() {
			return file.Data;
		}
		
		protected override void DrawViewer() {
			if(ImGui.RadioButton("Text", ref displayType, 0))
				GetLines(false);
			ImGui.SameLine();
			if(ImGui.RadioButton("Hex", ref displayType, 1))
				GetLines(true);
			
			
			ImGui.PushFont(UiBuilder.MonoFont);
			foreach(var line in lines)
				ImGui.TextUnformatted(line);
			ImGui.PopFont();
		}
		
		private void GetLines(bool hex) {
			var data = file.Data;
			
			if(hex) {
				var lineCount = (int)Math.Ceiling(data.Length / 16f);
				lines = new string[lineCount];
				for(var line = 0; line < lineCount; line++) {
					var hex2 = new StringBuilder(47, 47);
					for(int i = 0; i < 16; i++) {
						if(line * 16 + i > data.Length - 1)
							break;
						
						var h = data[line * 16 + i].ToString("X2");
						hex2.Insert(i * 3,     h[0]);
						hex2.Insert(i * 3 + 1, h[1]);
						if(i != 15)
							hex2.Insert(i * 3 + 2, ' ');
					}
					lines[line] = hex2.ToString();
				}
				
				displayType = 1;
				
				return;
			}
			
			var tmp = new List<string>();
			var cur = new StringBuilder(256);
			for(int i = 0; i < data.Length; i++) {
				var c = data[i];
				if(c == '\n') {
					tmp.Add(cur.ToString());
					cur.Clear();
				} else
					cur.Append((char)data[i]);
			}
			
			tmp.Add(cur.ToString());
			lines = tmp.ToArray();
			
			displayType = 0;
		}
	}
}