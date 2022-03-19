using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Aetherment.Util;

// TODO proper header to support mipmaps
// TODO https://www.khronos.org/opengl/wiki/S3_Texture_Compression

namespace Aetherment.Format {
	public class Tex {
		private struct Format {
			public uint Flags;
			public uint Flags2;
			public uint FourCC;
			public uint Bits; // or blocksize for those that use that instead
			public uint MaskR;
			public uint MaskG;
			public uint MaskB;
			public uint MaskA;
			
			public Format(uint flags, uint flags2, uint fourcc, uint bits, uint maskr, uint maskg, uint maskb, uint maska) {
				Flags = flags;
				Flags2 = flags2;
				FourCC = fourcc;
				Bits = bits;
				MaskR = maskr;
				MaskG = maskg;
				MaskB = maskb;
				MaskA = maska;
			}
		}
		
		// https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide
		private static Dictionary<short, Format> ddsFormatData = new() {
			{0x3420, new Format(0x00081007, 0x4,     0x31545844, 8,  0,          0,          0,          0         )}, // DXT1
			{0x3430, new Format(0x00081007, 0x4,     0x33545844, 16, 0,          0,          0,          0         )}, // DXT3
			{0x3431, new Format(0x00081007, 0x4,     0x35545844, 16, 0,          0,          0,          0         )}, // DXT5
			{0x2460, new Format(0x00081007, 0x4,     113,        64, 0,          0,          0,          0         )}, // R16G16B16A16F
			{0x1450, new Format(0x0000100F, 0x41,    0,          32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000)}, // A8R8G8B8
			{0x1451, new Format(0x0000100E, 0x40,    0,          32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0         )}, // X8R8G8B8
			{0x1440, new Format(0x0000100F, 0x41,    0,          16, 0x0F00u,    0x00F0u,    0x000Fu,    0xF000u   )}, // A4R4G4B4
			{0x1441, new Format(0x0000100F, 0x41,    0,          16, 0x7C00u,    0x03E0u,    0x001Fu,    0x8000u   )}, // A1R5G5B5
			{0x1130, new Format(0x0000100F, 0x20000, 0,          8,  0xFF,       0,          0,          0         )}, // L8
			{0x1131, new Format(0x0000100F, 0x2,     0,          8,  0,          0,          0,          0xFF      )}, // A8
		};
		
		public static void WriteFromDDS(string path, Buffer dataBuffer) {
			var file = File.Open(path, FileMode.Create);
			
			unsafe {
				var data = dataBuffer.Data;
				
				var format = GetFormatDDS(dataBuffer);
				var f = BitConverter.GetBytes(format);
				
				file.Write(new byte[80] {
					0,0, 128,0, f[0],f[1], 0,0, data[16],data[17], data[12],data[13], 1,0, 1,0,
					0,0,0,0, 1,0,0,0, 2,0,0,0, 80,0,0,0,
					0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
					0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
					0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0
				}, 0, 80);
			
				// for(int i = 128; i < Math.Min(dataBuffer.Size, ExpectedSizeDDS(Helper.ReadShort(data, 12), Helper.ReadShort(data, 16), format) + 128); i++)
				for(int i = 128; i < dataBuffer.Size; i++)
					file.WriteByte(data[i]);
			}
			
			file.Flush();
			file.Dispose();
		}
		
		public static void WriteToDDS(string path, Buffer dataBuffer) {
			var file = new BinaryWriter(File.Open(path, FileMode.Create));
			
			unsafe {
				var data = dataBuffer.Data;
				
				var mipmaps = Helper.ReadShort(data, 14);
				var f = Helper.ReadShort(data, 4);
				var format = ddsFormatData[f];
				
				var width = (uint)Helper.ReadShort(data, 8);
				var pitch = format.FourCC != 0 ? (width + 3) * format.Bits / 4 : (width * format.Bits + 7) / 8;
				
				file.Write((uint)0x20534444);
				file.Write((uint)124);
				file.Write((uint)(format.Flags | (mipmaps > 1 ? 0x2000 : 0)));
				file.Write(data[10]); file.Write(data[11]); file.Write((short)0);
				
				file.Write(data[8]); file.Write(data[9]); file.Write((short)0);
				file.Write(pitch);
				file.Write((uint)0);
				file.Write((uint)mipmaps);
				
				file.Write((uint)0x69206948);
				file.Write((uint)0x656D2074);
				file.Write((uint)0x76655320);
				file.Write((uint)0x63206969);
				
				file.Write((uint)0x3A);
				file.Write((uint)0);
				file.Write((uint)0);
				file.Write((uint)0);
				
				file.Write((uint)0);
				file.Write((uint)0);
				file.Write((uint)0);
				file.Write((uint)32);
				
				file.Write(format.Flags2);
				file.Write(format.FourCC);
				file.Write(format.Bits);
				file.Write(format.MaskR);
				
				file.Write(format.MaskG);
				file.Write(format.MaskB);
				file.Write(format.MaskA);
				file.Write((uint)0x1000); // todo, the 2 other flags
				
				file.Write((uint)0);
				file.Write((uint)0);
				file.Write((uint)0);
				file.Write((uint)0);
				
				for(int i = 80; i < dataBuffer.Size; i++)
					file.Write(data[i]);
			}
			
			file.Flush();
			file.Dispose();
		}
		
		public async static Task WriteFromPNG(string path, Buffer dataBuffer) {
			// todo
		}
		
		public async static Task WriteToPNG(string path, Buffer dataBuffer) {
			// todo
		}
		
		private unsafe static short GetFormatDDS(Buffer data) {
			var flags = Helper.ReadUInt(data.Data, 80);
			var cc = Helper.ReadUInt(data.Data, 84);
			
			switch(cc) {
				case 0x31545844:
					return 13344; // DXT 1
				case 0x33545844:
					return 13360; // DXT 3
				case 0x35545844:
					return 13361; // DXT 5
				case 0x71:
					return 9312; // A16B16G16R16F
				case 0:
					switch(flags) {
						case 2:
							return 4401; // A8
						case 65:
							var rmask = Helper.ReadUInt(data.Data, 92);
							switch(rmask) {
								case 16711680:
									return 5200; // A8R8G8B8
								case 31744:
									return 5185; // A1R5G5B5
								case 3840:
									return 5184; // A4R4G4B4
								default:
									return 0;
							}
						default:
							return 0;
					}
				default:
					return 0;
			}
		}
		
		public static int ExpectedSizeDDS(short width, short height, short format) {
			switch(format) {
				case 13344:
					return width * height / 2;
				case 13360:
					return width * height;
				case 13361:
					return width * height;
				case 9312:
					return width * height * 8;
				case 4401:
					return width * height;
				case 5200:
					return width * height * 4;
				case 5185:
					return width * height * 2;
				case 5184:
					return width * height * 2;
				default:
					// file has invalid format, possibly corrupt;
					return int.MaxValue;
			}
		}
		
		public unsafe static void Color(Buffer main, Vector4 color) {
			var data = main.Data;
			for(var i = 80; i <= main.Size - 4; i += 4) {
				data[i    ] = (byte)(data[i    ] * color.Z);
				data[i + 1] = (byte)(data[i + 1] * color.Y);
				data[i + 2] = (byte)(data[i + 2] * color.X);
				data[i + 3] = (byte)(data[i + 3] * color.W);
			}
		}
		
		public unsafe static void Overlay(Buffer main, Buffer overlay) {
			var m = main.Data;
			var o = overlay.Data;
			
			for(var i = 80; i <= main.Size - 4; i += 4) {
				var am = m[i + 3] / 255f;
				var ao = o[i + 3] / 255f;
				
				var a = ao + am * (1 - ao);
				m[i + 3] = (byte)(a * 255);
				for(int j = i; j < i + 3; j++)
					m[j] = (byte)((o[j] * ao + m[j] * am * (1 - ao)) / a);
			}
		}
		
		public static bool ResolveCustomizability(Dictionary<string, string> files, List<Mod.Option> options, string outPath) {
			if(files.Count == 1 && files.Keys.First().Split(".")[0] == "underlay")
				return false;
			
			// Order the list so things are overlayed in the correct order
			var files2 = files
				.OrderBy(x =>
					options.FindIndex(y => x.Key.Contains("_") && y is Mod.Option.Color && ((Mod.Option.Color)y).ID == x.Key.Split(".")[0].Split("_")[1]) +
					(x.Key.StartsWith("underlay_") ? 1000 : (x.Key.StartsWith("underlay") ? 2000 : 3000)));
			
			int size = 0;
			var mainBuffer = new Buffer();
			var mainBufferPtr = IntPtr.Zero;
			foreach(var file in files2) {
				var name = file.Key.Split(".")[0];
				var id = name.Contains("_") ? name.Split("_")[1] : null;
				var path = file.Value;
				
				var f = File.Open(path, FileMode.Open);
				if(size == 0) {
					f.Seek(8, SeekOrigin.Begin);
					size = (short)(f.ReadByte() | f.ReadByte() << 8) * (short)(f.ReadByte() | f.ReadByte() << 8) * 4 + 80;
				}
				
				unsafe {
					var dataPtr = Marshal.AllocHGlobal(size);
					var data = (byte*)dataPtr;
					
					f.Seek(0, SeekOrigin.Begin);
					for(var i = 0; i < size; i++)
						data[i] = (byte)f.ReadByte();
					
					var b = new Buffer(data, size);
					
					if(id != null) {
						var clr = ((Mod.Option.Color)options.First(x => x is Mod.Option.Color && ((Mod.Option.Color)x).ID == id)).Value;
						Color(b, clr);
					}
					
					if(mainBufferPtr == IntPtr.Zero) {
						mainBuffer = b;
						mainBufferPtr = dataPtr;
					} else {
						Overlay(mainBuffer, b);
						Marshal.FreeHGlobal(dataPtr);
					}
				}
				
				f.Dispose();
			}
			
			Directory.CreateDirectory(Path.GetDirectoryName(outPath));
			var outfile = File.Open(outPath, FileMode.Create);
			unsafe {
				var data = mainBuffer.Data;
				for(int i = 0; i < size; i++)
				// for(int i = 0; i < 80; i++)
					outfile.WriteByte(data[i]);
			}
			outfile.Flush();
			outfile.Dispose();
			Marshal.FreeHGlobal(mainBufferPtr);
			
			return true;
		}
	}
}