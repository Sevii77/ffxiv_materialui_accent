using System;
using System.Linq;
using System.Text;
using System.Numerics;

using ImGuiNET;
using Dalamud.Interface;

using Lumina.Data;

namespace Aetherment.GUI.Explorer {
	internal class Raw : Viewer {
		private FileResource file;
		private string[] hex;
		
		public Raw(ulong hash, string path) : base(hash, path) {
			var ext = "." + path.Split(".").Last();
			importValids = new(){ext};
			exportValids = new(){ext};
			
			try {
				file = GetFile<FileResource>(hash, path);
				
				var data = file.Data;
				var lines = (int)Math.Ceiling(data.Length / 16f);
				hex = new string[lines];
				for(var line = 0; line < lines; line++) {
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
					hex[line] = hex2.ToString();
				}
			} catch(Exception e) {
				ShowError(e.ToString());
			}
		}
		
		public override byte[] GetFileData() {
			return file.Data;
		}
		
		protected override void DrawViewer() {
			ImGui.Dummy(Vector2.Zero);
			ImGui.PushFont(UiBuilder.MonoFont);
			foreach(var line in hex)
				ImGui.Text(line);
			ImGui.PopFont();
		}
	}
}