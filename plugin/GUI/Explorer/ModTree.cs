using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using ImGuiNET;

using Aetherment.Util;

namespace Aetherment.GUI.Explorer {
	internal class ModTree {
		public struct Dir {
			public struct File {
				public struct Opt {
					public string RealPath;
					public string Group;
					public string Option;
				}
				
				public string Name;
				public string GamePath;
				public List<Opt> Options;
			}
			
			// public struct File {
			// 	public string Name;
			// 	public string RealPath;
			// 	public string GamePath;
			// 	public string Group;
			// 	public string Option;
			// }
			
			public string Name = "";
			public List<Dir> Dirs = new();
			public List<File> Files = new();
		}
		
		private string modid;
		private string name;
		private Action<Dir.File> callback;
		private Dir files;
		private Dictionary<string, Dictionary<string, Dictionary<string, string>>> optionFiles;
		private string selected;
		
		public ModTree(string modid, Action<Dir.File> callback) {
			this.modid = modid;
			name = PenumbraApi.GetModSortOrder(modid).Split("/").Last();
			this.callback = callback;
			
			BuildNodes();
		}
		
		private void BuildNodes() {
			files = new Dir{Name = "Files"};
			optionFiles = PenumbraApi.GetModFiles(modid);
			
			foreach(var group in optionFiles)
				foreach(var option in group.Value)
					foreach(var path in option.Value) {
						var segs = path.Value.Split("/");
						var dir = files;
						for(int i = 0; i < segs.Length - 1; i++)
							try {
								dir = dir.Dirs.First(x => x.Name == segs[i]);
							} catch {
								dir.Dirs.Add(new Dir{Name = segs[i]});
								dir = dir.Dirs.Last();
							}
						
						Dir.File file;
						try {
							file = dir.Files.First(x => x.GamePath == path.Value);
						} catch {
							file = new Dir.File{
								Name = segs.Last(),
								GamePath = path.Value,
								Options = new()
							};
							dir.Files.Add(file);
						}
						
						file.Options.Add(new Dir.File.Opt{
							RealPath = Path.Combine(PenumbraApi.GetModDirectory(Aetherment.Config.ExplorerMod).FullName, path.Key),
							Group = group.Key,
							Option = option.Key
						});
					}
			
			static void OrderDir(Dir dir) {
				dir.Dirs.Sort((x, y) => x.Name.CompareTo(y.Name));
				dir.Files.Sort((x, y) => x.Name.CompareTo(y.Name));
				
				foreach(var d in dir.Dirs)
					OrderDir(d);
			}
			
			OrderDir(files);
		}
		
		private void DrawNode(Dir dir) {
			foreach(var d in dir.Dirs)
				if(ImGui.TreeNode(d.Name)) {
					DrawNode(d);
					ImGui.TreePop();
				}
			
			foreach(var f in dir.Files)
				if(ImGui.Selectable($"{f.Name}", f.GamePath == selected)) {
					selected = f.GamePath;
					callback(f);
				}
		}
		
		public void Draw() {
			if(ImGui.TreeNode(name)) {
				DrawNode(files);
				ImGui.TreePop();
			}
		}
	}
}