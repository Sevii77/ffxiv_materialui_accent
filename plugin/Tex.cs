// TODO: Maby allow non A8R8G8B8 to be colored by decompressing them (https://www.khronos.org/opengl/wiki/S3_Texture_Compression)

using System;
using System.IO;
using System.Numerics;

namespace MaterialUI {
	public class Tex {
		public byte[] header {get; private set;}
		public byte[] body {get; private set;}
		
		public Tex(Tex tex) {
			header = (byte[])tex.header.Clone();
			body = (byte[])tex.body.Clone();
		}
		
		public Tex(byte[] dds) {
			BinaryReader reader = new BinaryReader(new MemoryStream(dds));
			
			reader.BaseStream.Seek(12, SeekOrigin.Begin);
			byte[] h = BitConverter.GetBytes((short)reader.ReadInt32());
			byte[] w = BitConverter.GetBytes((short)reader.ReadInt32());
			byte[] f = BitConverter.GetBytes(GetFormat(reader));
			
			// https://docs.google.com/document/d/1-874zlRLfxGyVei3XR0UHTpxdLnmqJwKms2kGFSBAQo
			header = new byte[80] {
				0,0, 128,0, f[0],f[1], 0,0, w[0],w[1], h[0],h[1], 1,0, 1,0,
				0,0,0,0, 1,0,0,0, 2,0,0,0, 80,0,0,0,
				0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
				0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
				0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0
			};
			
			
			body = new byte[dds.Length - 128];
			for(int i = 128; i < dds.Length; i++)
				body[i - 128] = dds[i];
		}
		
		private short GetFormat(BinaryReader dds) {
			// https://github.com/TexTools/xivModdingFramework/blob/d870525ec57141cd03c82f40fe60d5eed90827c0/xivModdingFramework/Textures/FileTypes/DDS.cs#L93
			// https://github.com/TexTools/xivModdingFramework/blob/e8fc9d06331d5f05595ee81789569b376f904436/xivModdingFramework/SqPack/FileTypes/Dat.cs#L1990
			dds.BaseStream.Seek(80, SeekOrigin.Begin);
			uint flags = dds.ReadUInt32();
			uint cc = dds.ReadUInt32();
			
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
							dds.ReadUInt32();
							uint rmask = dds.ReadUInt32();
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
		
		// This assumes the format is A8R8G8B8
		public void Paint(Vector3 clr) {
			float r = clr.X;
			float g = clr.Y;
			float b = clr.Z;
			
			for(int i = 0; i <= body.Length - 4; i += 4) {
				body[i    ] = (byte)(body[i    ] * b);
				body[i + 1] = (byte)(body[i + 1] * g);
				body[i + 2] = (byte)(body[i + 2] * r);
			}
		}
		
		public void Overlay(Tex overlay) {
			byte[] over = overlay.body;
			
			for(int i = 0; i <= body.Length - 4; i += 4) {
				float ab = body[i + 3] / 255f;
				float ao = over[i + 3] / 255f;
				
				body[i + 3] += (byte)(over[i + 3] * (1f - ab));
				for(int j = i; j < i + 3; j++) {
					body[j] = (byte)((body[j] * ab + over[j] * ao) / (ab + ao));
				}
			}
		}
		
		public void Save(string path) {
			using(BinaryWriter f = new BinaryWriter(File.Open(path, FileMode.Create))) {
				f.Write(header);
				f.Write(body);
			}
		}
	}
}