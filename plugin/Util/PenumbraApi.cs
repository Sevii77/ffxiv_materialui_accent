using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Dalamud;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Aetherment.Util {
	public class PenumbraApi {
		public static bool IsPenumbraLoaded() {
			try {
				Aetherment.Interface.GetIpcSubscriber<int>("Penumbra.ApiVersion").InvokeFunc();
				return true;
			} catch {
				return false;
			}
		}
		
		private static ICallGateSubscriber<DirectoryInfo> modDirectory = null;
		public static DirectoryInfo GetDirectory() {
			if(modDirectory == null)
				modDirectory = Aetherment.Interface.GetIpcSubscriber<DirectoryInfo>("Penumbra.ModDirectory");
			
			return modDirectory.InvokeFunc();
		}
		
		private static ICallGateSubscriber<string, object> addOrUpdateMod = null;
		public static void RefreshMod(string modId) {
			if(addOrUpdateMod == null)
				addOrUpdateMod = Aetherment.Interface.GetIpcSubscriber<string, object>("Penumbra.AddOrUpdateMod");
			
			addOrUpdateMod.InvokeAction(modId);
		}
		
		public static void RefreshMod(Mod mod)
			=> RefreshMod(mod.ID);
		
		private static ICallGateSubscriber<string[]> getMods = null;
		public static string[] GetMods() {
			if(getMods == null)
				getMods = Aetherment.Interface.GetIpcSubscriber<string[]>("Penumbra.GetMods");
			
			return getMods.InvokeFunc();
		}
		
		public static DirectoryInfo GetModDirectory(string modId) {
			return new DirectoryInfo($"{GetDirectory().FullName}/{modId}");
		}
		
		public static DirectoryInfo GetModDirectory(Mod mod)
			=> GetModDirectory(mod.ID);
		
		private static ICallGateSubscriber<string, string> getModSortOrder = null;
		public static string GetModSortOrder(string modId) {
			if(getModSortOrder == null)
				getModSortOrder = Aetherment.Interface.GetIpcSubscriber<string, string>("Penumbra.GetModSortOrder");
			
			return getModSortOrder.InvokeFunc(modId);
		}
		
		public static string GetModSortOrder(Mod mod)
			=> GetModSortOrder(mod.ID);
		
		private static ICallGateSubscriber<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> getModFiles = null;
		public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> GetModFiles(string modId) {
			if(getModFiles == null)
				getModFiles = Aetherment.Interface.GetIpcSubscriber<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>("Penumbra.GetModFiles");
			
			return getModFiles.InvokeFunc(modId);
		}
		
		public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> GetModFiles(Mod mod)
			=> GetModFiles(mod.ID);
	}
}