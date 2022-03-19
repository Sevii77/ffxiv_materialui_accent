using System.IO;
using System.Collections.Generic;

using Aetherment.Util;

namespace Aetherment.Format {
	public class Raw {
		public unsafe static void WriteRaw(string path, Buffer dataBuffer) {
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			var file = File.Open(path, FileMode.Create);
			
			unsafe {
				var data = dataBuffer.Data;
				for(int i = 0; i < dataBuffer.Size; i++)
					file.WriteByte(data[i]);
			}
			
			file.Flush();
			file.Dispose();
		}
		
		public static void WriteFrom(string ext, string path, Buffer dataBuffer) {
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			
			switch(ext.ToLower()) {
				case "dds":
					Tex.WriteFromDDS(path, dataBuffer);
					break;
				case "png":
					Tex.WriteFromDDS(path, dataBuffer);
					break;
				default:
					WriteRaw(path, dataBuffer);
					break;
			}
		}
		
		public static void WriteTo(string ext, string path, Buffer dataBuffer) {
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			
			switch(ext.ToLower()) {
				case "dds":
					Tex.WriteToDDS(path, dataBuffer);
					break;
				case "png":
					Tex.WriteToDDS(path, dataBuffer);
					break;
				default:
					WriteRaw(path, dataBuffer);
					break;
			}
		}
		
		public static bool ResolveCustomizability(string ext, Dictionary<string, string> files, List<Mod.Option> options, string outpath) {
			switch(ext.ToLower()) {
				case "tex":
					Tex.ResolveCustomizability(files, options, outpath);
					return true;
				default:
					return false;
			}
		}
	}
}