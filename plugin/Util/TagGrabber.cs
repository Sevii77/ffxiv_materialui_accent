using System.Collections.Generic;
using System.Text.RegularExpressions;

using Dalamud.Logging;

namespace Aetherment.Util {
	public class TagGrabber {
		private static Dictionary<string, string[]> pathTags = new() {
			{"ui", new string[] {
				@"^ui/"
			}},
			
			{"4k", new string[] {
				@"^ui/uld/[a-z0-9_]+_hr1.tex",
				@"^ui/uld/(?:light|third)/[a-z0-9_]+_hr1.tex",
				@"^ui/icon/\d{6}/\d{6}_hr1.tex",
				@"^ui/icon/\d{6}/hq/\d{6}_hr1.tex"
			}},
			
			{"hd", new string[] {
				@"^ui/uld/[a-z0-9_]+(?:^_hr1).tex",
				@"^ui/uld/(?:light|third)/[a-z0-9_]+(?:^_hr1).tex",
				@"^ui/icon/\d{6}/\d{6}.tex",
				@"^ui/icon/\d{6}/hq/\d{6}.tex"
			}},
			
			{"gauge", new string[] {
				@"^ui/uld/parameter_gauge",
				@"^ui/uld/contentgauge",
				@"^ui/uld/limitbreak",
				@"^ui/uld/jobhud"
			}},
			
			{"icon", new string[] {
				@"^ui/icon/\d{6}/\d{6}",
				@"^ui/icon/\d{6}/hq/\d{6}"
			}},
			
			{"map", new string[] {
				@"^ui/map/"
			}},
			
			{"window", new string[] {
				@"^ui/uld/[a-z0-9_]+_corner(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+_h(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+_hv(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+_v(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+bgcorner(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+bgh(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+bghv(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+bgv(?:_hr1)?.tex",
				@"^ui/uld/[a-z0-9_]+_bg(?:_hr1)?.tex",
				@"^ui/uld/achievementbg(?:_hr1)?.tex",
				@"^ui/uld/housingguestbook(?:_hr1)?.tex",
				@"^ui/uld/minidungeonwindow(?:_hr1)?.tex",
				@"^ui/uld/weeklybingobg(?:_hr1)?.tex"
			}},
			
			{"cursor", new string[] {
				@"^ui/uld/targetcursor"
			}}
		};
		
		public static List<string> GrabTags(Mod mod) {
			var tags = new List<string>();
			
			foreach(var option in mod.Options)
				// if(!(option is Mod.Option.Single || option is Mod.Option.Multi)) {
				if(option is not Mod.Option.Penumbra) {
					tags.Add("customizable");
					
					break;
				}
			
			void checkPath(string path) {
				foreach(KeyValuePair<string, string[]> tagPaths in pathTags) {
					if(tags.Contains(tagPaths.Key))
						continue;
					
					foreach(var pattern in tagPaths.Value)
						if(Regex.IsMatch(path, pattern)) {
							tags.Add(tagPaths.Key);
							
							break;
						}
				}
			}
			
			void walk(Dir dir, string path) {
				foreach(Dir sub in dir.Dirs.Values)
					if(sub.Name.Contains("."))
						checkPath(path + sub.Name);
					else
						walk(sub, path + sub.Name + "/");
				
				foreach(Dir.File file in dir.Files.Values)
					checkPath(path + file.Name);
			}
			
			walk(mod.Files.Dirs["files"], "");
			
			return tags;
		}
		
		public static void AddTags(Mod mod) {
			foreach(var tag in GrabTags(mod))
				mod.Tags.Add(tag);
		}
	}
}