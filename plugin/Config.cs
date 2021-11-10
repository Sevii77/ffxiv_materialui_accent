using Dalamud.Configuration;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace MaterialUI {
	[Serializable]
	public class Config : IPluginConfiguration {
		public int Version {get; set;} = 0;
		
		public bool firstTime = true;
		public bool openOnStart = true;
		public string style = "black";
		public bool accentOnly {get; set;} = false;
		public Vector3 color {get; set;} = new Vector3(99 / 255f, 60 / 255f, 181 / 255f);
		public Dictionary<string, Vector3> colorOptions {get; set;} = new Dictionary<string, Vector3>();
	}
}