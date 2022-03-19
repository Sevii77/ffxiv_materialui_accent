using System;
using System.Collections.Generic;

using ImGuiNET;
using Dalamud.Interface;

using Lumina.Data;

namespace Aetherment.GUI.Explorer {
	internal class Selector : Viewer {
		private Viewer viewer;
		private string typename;
		
		public Selector(ulong hash, string path) : base(hash, path) {
			try {
				var file = GetFile<FileResource>(hash, path);
				
				foreach(var type in Explorer.ViewerCreators) {
					for(int i = 0; i < type.Item3.Length; i++)
						if(type.Item3[i] != file.Data[i])
							goto skip;
					
					UpdateViewer(type);
					break;
					
					skip: {}
				}
			} catch(Exception e) {
				viewer = new Raw(hash, path);
			}
		}
		
		public override void Dispose() {
			viewer.Dispose();
		}
		
		public override byte[] GetFileData()
			=> viewer.GetFileData();
		
		protected override void DrawViewer() {
			ImGui.SetNextItemWidth(75 * ImGuiHelpers.GlobalScale);
			if(ImGui.BeginCombo("##typeselector", typename, ImGuiComboFlags.HeightRegular)) {
				foreach(var type in Explorer.ViewerCreators)
					if(ImGui.Selectable(type.Item1, type.Item1 == typename))
						UpdateViewer(type);
				
				ImGui.EndCombo();
			}
			
			ImGui.SameLine();
			viewer.Draw();
		}
		
		private void UpdateViewer((string, string, byte[], Func<ulong, string, Viewer>) type) {
			if(viewer != null)
				viewer.Dispose();
			
			viewer = type.Item4(hash, path);
			typename = type.Item1;
			
			importValids = viewer.importValids;
			exportValids = viewer.exportValids;
		}
	}
}