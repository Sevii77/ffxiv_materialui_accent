using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using ImGuiNET;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;

using Lumina;

using Aetherment.Util;

namespace Aetherment.GUI.Explorer {
	internal class Explorer : IDisposable {
		private struct ModDir {
			public string Name;
			public List<ModDir> Dirs;
			public Dictionary<string, string> Mods;
			
			public ModDir(string name) {
				Name = name;
				Dirs = new();
				Mods = new();
			}
			
			public ModDir AddDir(string name) {
				foreach(var dir in Dirs)
					if(dir.Name == name)
						return dir;
				
				var d = new ModDir(name);
				Dirs.Add(d);
				
				return d;
			}
		}
		
		public static readonly (string, string, byte[], Func<ulong, string, Viewer>)[] ViewerCreators = new (string, string, byte[], Func<ulong, string, Viewer>)[] {
			("Texture", "a?tex", new byte[4]{0, 0, 128, 0}, (hash, path) => new Tex(hash, path)),
			("Binary",  ".+",    new byte[0]{            }, (hash, path) => new Raw(hash, path))
		};
		
		private ModTree modTree;
		private GameTree gameTree;
		private Viewer viewer;
		
		// private FileDialogManager dialog;
		private FileDialog dialog;
		private Action<bool, string> dialogCallback;
		private string dialogFilters;
		
		private string curPath = "";
		private bool validPath = false;
		
		private string newMod = "";
		private ModDir mods;
		
		public Explorer() {
			gameTree = new(OpenFile);
			viewer = new(0, "");
			// dialog = new();
			
			ReloadMods();
		}
		
		public void Dispose() {
			gameTree.Dispose();
			viewer.Dispose();
		}
		
		public void Draw() {
			ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));
			ImGui.BeginTable("##divider", 2, ImGuiTableFlags.Resizable);
			ImGui.TableSetupColumn("##tree", ImGuiTableColumnFlags.WidthFixed, 200);
			ImGui.TableSetupColumn("##viewer", ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableNextRow();
			
			var h = ImGuiAeth.Height();
			var size = new Vector2(0, -(h + ImGuiAeth.SpacingY));
			
			// Tree
			ImGui.TableNextColumn();
			ImGui.BeginChildFrame(ImGui.GetID("tree"), size);
			if(modTree != null) {
				modTree.Draw();
				ImGuiAeth.Offset(0, 20);
			}
			gameTree.Draw();
			ImGui.EndChildFrame();
			
			// Mod selector
			ImGui.SetNextItemWidth((uint)ImGuiAeth.WidthLeft());
			if(ImGui.BeginCombo("##modselector", Aetherment.Config.ExplorerMod, ImGuiComboFlags.HeightRegular)) {
				if(ImGuiAeth.ButtonIcon(FontAwesomeIcon.Plus) && newMod != "") {
					if(!PenumbraApi.GetMods().Contains(newMod)) {
						
					}
					
					newMod = "";
				}
				ImGui.SameLine();
				ImGuiAeth.Offset(-ImGuiAeth.SpacingX, 0, false);
				var w = (uint)ImGuiAeth.WidthLeft();
				ImGui.SetNextItemWidth(w);
				ImGui.InputTextWithHint("", "New Mod (TODO)", ref newMod, w);
				
				ImGuiAeth.Offset(0, 10);
				DrawModTree(mods);
				
				ImGui.EndCombo();
			}
			if(ImGui.IsItemClicked())
				ReloadMods();

			// Viewer
			ImGui.TableNextColumn();
			ImGui.BeginChild("viewer", size);
			viewer.Draw();
			ImGui.EndChild();
			
			// Viewer bottom toolbar
			if(validPath) {
				if(ImGui.Button("Import (TODO)", new Vector2(100, h))) {
					var name = curPath.Split("/").Last();
					OpenDialog("OpenFileDialog", "Import " + name, string.Join(",", viewer.exportValids), ".", Import);
					// dialog.OpenFileDialog("Import " + name, string.Join(",", viewer.exportValids), Import);
					// dialog.OpenFileDialog("Import " + name, string.Join(",", viewer.exportValids.Select(x => $"{x.Value}{{.{x.Key}}}")), Import);
				}
				
				ImGui.SameLine();
				if(ImGui.Button("Export", new Vector2(100, h))) {
					var name = curPath.Split("/").Last().Split(".")[0];
					OpenDialog("SaveFileDialog", "Export " + name, string.Join(",", viewer.exportValids), name, Export);
					// dialog.SaveFileDialog("Export " + name, string.Join(",", viewer.exportValids), name, name.Split(".").Last(), Export);
					// dialog.SaveFileDialog("Export " + name, string.Join(",", viewer.exportValids.Select(x => $"{x.Value}{{.{x.Key}}}")), name, name.Split(".").Last(), Export);
				}
				
				ImGui.SameLine();
			}
			
			var isred = !validPath;
			if(isred)
				ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.DalamudRed);
			ImGui.SetNextItemWidth((uint)ImGuiAeth.WidthLeft());
			if(ImGui.InputTextWithHint("", "Path", ref curPath, 128))
				OpenFile(curPath);
			if(isred)
				ImGui.PopStyleColor();
			
			ImGui.EndTable();
			ImGui.PopStyleVar();
			
			if(dialog != null && dialog.Draw()) {
				var result = dialog.GetResult();
				dialogCallback(dialog.GetIsOk(), result);
				Aetherment.Config.ExplorerExportPath = dialog.GetCurrentPath();
				if(dialogFilters != null)
					Aetherment.Config.ExplorerExportExt[dialogFilters] = "." + result.Split(".").Last();
				Aetherment.SaveConfig();
				dialog = null;
			}
				
		}
		
		private void DrawModTree(ModDir mods) {
			foreach(var d in mods.Dirs)
				if(ImGui.TreeNode(d.Name)) {
					DrawModTree(d);
					ImGui.TreePop();
				}
			
			foreach(var mod in mods.Mods)
				if(ImGui.Selectable(mod.Key, Aetherment.Config.ExplorerMod == mod.Value)) {
					Aetherment.Config.ExplorerMod = mod.Value;
					Aetherment.SaveConfig();
					ReloadSelectedMod();
				}
		}
		
		private void ReloadMods() {
			var modIds = PenumbraApi.GetMods();
			if(!modIds.Contains(Aetherment.Config.ExplorerMod)) {
				Aetherment.Config.ExplorerMod = "";
				Aetherment.SaveConfig();
			}
			
			mods = new("");
			foreach(var mod in modIds) {
				var path = PenumbraApi.GetModSortOrder(mod).Split('/');
				
				var curDir = mods;
				for(int i = 0; i < path.Length - 1; i++)
					curDir = curDir.AddDir(path[i]);
				
				curDir.Mods[path.Last()] = mod;
			}
			
			ReloadSelectedMod();
		}
		
		private void ReloadSelectedMod() {
			if(Aetherment.Config.ExplorerMod != "")
				modTree = new(Aetherment.Config.ExplorerMod, OpenFile);
			else
				modTree = null;
		}
		
		private void OpenFile(ModTree.Dir.File file) {
			viewer.Dispose();
			var v = new OptionSelector(0, file.Options[0].RealPath);
			v.SetFile(file);
			viewer = v;
			
			validPath = true;
			curPath = file.GamePath;
		}
		
		private void OpenFile(string path) {
			try {
				OpenFile(GameData.GetFileHash(curPath), path);
				curPath = path;
			} catch {
				validPath = false;
			}
		}
		
		private void OpenFile(ulong hash, string path) {
			var exists = false;
			foreach(var exp in Aetherment.GameData.Repositories)
				foreach(var cat in exp.Value.Categories)
					foreach(var ind in cat.Value)
						if(!ind.Index.IsIndex2 && ind.IndexHashTableEntries.ContainsKey(hash)) {
							exists = true;
							break;
						}
			
			if(!exists) {
				validPath = false;
				return;
			}
			
			viewer.Dispose();
			
			var ext = path.Trim().Split(".").Last();
			foreach(var creator in ViewerCreators)
				if(Regex.IsMatch(ext, creator.Item2)) {
					viewer = creator.Item4(hash, path);
					break;
				}
			
			validPath = true;
			curPath = path;
		}
		
		private void Import(bool success, string path) {
			if(!success)
				return;
			
			
		}
		
		private void Export(bool success, string path) {
			if(!success)
				return;
			
			var ext = path.Split(".").Last();
			var data = viewer.GetFileData();
			unsafe {
				fixed(byte* d = &data[0])
					Format.Raw.WriteTo(ext, path, new(d, data.Length));
			}
		}
		
		private void OpenDialog(string id, string title, string filters, string name, Action<bool, string> callback) {
			var ext = string.Empty;
			if(id == "SaveFileDialog") {
				ext = Aetherment.Config.ExplorerExportExt.ContainsKey(filters) ? Aetherment.Config.ExplorerExportExt[filters] : filters.Split(",")[0];
				dialogFilters = filters;
			} else
				dialogFilters = null;
			
			dialog = new FileDialog(id, title, filters, Aetherment.Config.ExplorerExportPath, name, ext, 1, false, ImGuiFileDialogFlags.None);
			dialog.Show();
			dialogCallback = callback;
		}
	}
}