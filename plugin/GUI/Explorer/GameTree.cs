using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

using ImGuiNET;

namespace Aetherment.GUI.Explorer {
	internal class GameTree : IDisposable {
		private struct Dir {
			public string Name;
			public long ID;
		}
		
		private struct File {
			public string Name;
			public ulong Hash;
		}
		
		private struct Node {
			public List<Dir> Dirs;
			public List<File> Files;
		}
		
		// private SqliteConnection db;
		private Dictionary<long, Node> pathCache = new();
		private ulong selected = 0;
		private Action<ulong, string> callback;
		
		public GameTree(Action<ulong, string> callback) {
			// db = new SqliteConnection($"Data Source={Aetherment.Interface.AssemblyLocation.DirectoryName}/assets/paths.db;Mode=ReadOnly");
			// SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
			this.callback = callback;
		}
		
		public void Dispose() {
			// db.Dispose();
		}
		
		private Node GetGameNode(long id) {
			if(!pathCache.ContainsKey(id)) {
				var db = new SqliteConnection($"Data Source={Aetherment.Interface.AssemblyLocation.DirectoryName}/assets/paths.db;Mode=ReadOnly");
				db.Open();
				
				var dirs = new List<Dir>();
				var c = db.CreateCommand();
				c.CommandText = $"SELECT rowid, name FROM dirs WHERE parent = {id}";
				using(var r = c.ExecuteReader())
					while(r.Read())
						dirs.Add(new Dir{
							Name = r.GetString(1),
							ID = r.GetInt64(0)
						});
				c.Dispose();
				dirs = dirs.OrderBy(x => x.Name).ToList();
				
				var files = new List<File>();
				c = db.CreateCommand();
				c.CommandText = $"SELECT name, hash FROM files WHERE parent = {id}";
				using(var r = c.ExecuteReader())
					while(r.Read())
						files.Add(new File{
							Name = r.GetString(0),
							Hash = (ulong)r.GetInt64(1)
						});
				c.Dispose();
				files = files.OrderBy(x => x.Name).ToList();
				
				pathCache[id] = new Node{
					Dirs = dirs,
					Files = files
				};
				
				db.Close();
				db.Dispose();
			}
			
			return pathCache[id];
		}
		
		private void Search(string search) {
			// TODO: this
			// smth like this mby
			// "phics/texture/-mog"
			// SELECT * FROM files WHERE name LIKE "-mog%%" AND parent IS (SELECT rowid FROM dirs WHERE name IS "texture" AND parent IS (SELECT rowid FROM dirs WHERE name LIKE "%%phics"))
		}
		
		private void DrawNode(long id, string path) {
			var node = GetGameNode(id);
			
			foreach(var d in node.Dirs)
				if(ImGui.TreeNode(d.Name)) {
					DrawNode(d.ID, path + d.Name + "/");
					ImGui.TreePop();
				}
			
			foreach(var f in node.Files)
				if(ImGui.Selectable(f.Name, f.Hash == selected)) {
					selected = f.Hash;
					callback(f.Hash, path + f.Name);
				}
		}
		
		public void Draw() {
			if(ImGui.TreeNode("Game Files")) {
				DrawNode(0, "");
				ImGui.TreePop();
			}
		}
	}
}