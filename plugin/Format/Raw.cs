using System.IO;

namespace Aetherment.Format {
	public class Raw {
		public unsafe static void Write(string path, Buffer dataBuffer) {
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
	}
}