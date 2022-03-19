using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Interface.Colors;

using Lumina.Data;

namespace Aetherment.GUI.Explorer {
	internal class Viewer : IDisposable {
		public List<string> importValids;
		public List<string> exportValids;
		
		protected readonly ulong hash;
		protected readonly string path;
		
		private string error;
		
		public Viewer(ulong hash, string path) {
			this.hash = hash;
			this.path = path;
		}
		
		protected T? GetFile<T>(ulong hash, string path) where T : FileResource {
			try {
				return GetFile<T>(hash);
			} catch {
				return GetFile<T>(path);
			}
		}
		
		private T? GetFile<T>(ulong hash) where T : FileResource {
			foreach(var exp in Aetherment.GameData.Repositories)
				foreach(var cat in exp.Value.Categories)
					foreach(var ind in cat.Value)
						if(!ind.Index.IsIndex2 && ind.IndexHashTableEntries.ContainsKey(hash))
							return ind.GetFile<T>(hash);
			
			throw new Exception("File not found");
		}
		
		private T? GetFile<T>(string path) where T : FileResource {
			return Aetherment.GameData.GetFileFromDisk<T>(path);
		}
		
		public void ShowError(string error) {
			this.error = error;
		}
		
		public void Draw() {
			if(error != null) {
				ImGui.Dummy(Vector2.Zero);
				ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
				ImGui.Text(error);
				ImGui.PopStyleColor();
				
				return;
			}
			
			DrawViewer();
		}
		
		public virtual void Dispose() {}
		public virtual byte[] GetFileData() {
			return new byte[0];
		}
		
		protected virtual void DrawViewer() {}
	}
}