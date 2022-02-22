namespace Aetherment.Format {
	public unsafe struct Buffer {
		public byte* Data;
		public int Size;
		
		public Buffer(byte* data, int size) {
			Data = data;
			Size = size;
		}
	}
	
	public class Helper {
		public unsafe static short ReadShort(byte* data, int o) {
			return (short)(data[o + 1] << 8 | data[o]);
		}
		
		public unsafe static uint ReadUInt(byte* data, int o) {
			return (uint)(data[o + 3] << 24 | data[o + 2] << 16 | data[o + 1] << 8 | data[o]);
		}
	}
}