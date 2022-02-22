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
		public unsafe static void ConvertFromDDS(string path, Buffer dataBuffer) {
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			var file = File.Open(path, FileMode.Create);
			
			unsafe {
				var data = dataBuffer.Data;
				
				var format = GetFormatDDS(data);
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
		
		private unsafe static short GetFormatDDS(byte* data) {
			var flags = Helper.ReadUInt(data, 80);
			var cc = Helper.ReadUInt(data, 84);
			
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
							var rmask = Helper.ReadUInt(data, 92);
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
		
		public async static Task ConvertFromPNG(string path, Buffer dataBuffer) {
			// todo
		}
	}
}