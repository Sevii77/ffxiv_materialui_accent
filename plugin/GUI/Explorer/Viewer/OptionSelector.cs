using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using ImGuiNET;
using Dalamud.Interface;

using Lumina.Data;

namespace Aetherment.GUI.Explorer {
	internal class OptionSelector : Viewer {
		private Viewer viewer;
		private ModTree.Dir.File file;
		private string group;
		private string option;
		
		public OptionSelector(ulong hash, string path) : base(hash, path) {
			
		}
		
		public override void Dispose() {
			viewer.Dispose();
		}
		
		public override byte[] GetFileData()
			=> viewer.GetFileData();
		
		public void SetFile(ModTree.Dir.File file) {
			this.file = file;
			group = file.Options[0].Group;
			option = file.Options[0].Option;
			
			UpdateViewer();
		}
		
		protected override void DrawViewer() {
			if(file.Options.Count > 1) {
				ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
				if(ImGui.BeginCombo("##optionselector", $"{group}/{option}", ImGuiComboFlags.HeightRegular)) {
					foreach(var opt in file.Options)
						if(ImGui.Selectable($"{opt.Group}/{opt.Option}", opt.Group == group && opt.Option == option)) {
							group = opt.Group;
							option = opt.Option;
							UpdateViewer();
						}
					
					ImGui.EndCombo();
				}
				
				ImGui.SameLine();
			}
			
			viewer.Draw();
		}
		
		private void UpdateViewer() {
			if(viewer != null)
				viewer.Dispose();
			
			var ext = file.GamePath.Split(".").Last();
			foreach(var creator in Explorer.ViewerCreators)
				if(Regex.IsMatch(ext, creator.Item2)) {
					viewer = creator.Item4(hash, file.Options.First(x => x.Group == group && x.Option == option).RealPath);
					break;
				}
			
			importValids = viewer.importValids;
			exportValids = viewer.exportValids;
		}
	}
}