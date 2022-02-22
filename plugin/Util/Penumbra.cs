using System.Reflection;

using Dalamud;
using Dalamud.Plugin;
using Dalamud.Logging;

namespace Aetherment.Util {
	public class Penumbra {
		// Im sure theres a way to do this without inf+1 reflections and casting it (glamourer does it)
		// but when i tried that it cried about it being a different penumbra assembly, so eh fuck it
		
		private static IDalamudPlugin GetPenumbra() {
			var ass = typeof(ClientLanguage).Assembly;
			var typePluginManager = ass.GetType("Dalamud.Plugin.Internal.PluginManager");
			var typeLocalPlugin = ass.GetType("Dalamud.Plugin.Internal.LocalPlugin");
			var pluginManager = ass.GetType("Dalamud.Service`1").MakeGenericType(typePluginManager)
				.GetMethod("Get").Invoke(null, BindingFlags.Default, null, new object[] {}, null);
			
			var plugins = (System.Collections.IEnumerable)typePluginManager.GetProperty("InstalledPlugins").GetValue(pluginManager);
			foreach(var plugin in plugins)
				if((string)typeLocalPlugin.GetProperty("Name").GetValue(plugin) == "Penumbra")
					return (IDalamudPlugin)typeLocalPlugin.GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(plugin);
			
			return null;
		}
		
		public static string GetModPath() {
			var config = GetPenumbra().GetType().GetProperty("Config", BindingFlags.Public | BindingFlags.Static).GetValue(null);
			return (string)config.GetType().GetProperty("ModDirectory").GetValue(config);
		}
		
		public static void RefreshMod(Mod mod) {
			// todo
		}
		
		// public static void GetModOptionValues(Mod mod) {
		// 	var penumbra = GetPenumbra();
		// 	var ass = penumbra.GetType().Assembly;
		// 	var typeModManager = ass.GetType("Penumbra.Mods.ModManager");
		// 	var typeModData = ass.GetType("Penumbra.Mod.ModData");
		// 	var modManager = ass.GetType("Penumbra.Util.Service`1").MakeGenericType(typeModManager)
		// 		.GetMethod("Get").Invoke(null, BindingFlags.Default, null, new object[] {}, null);
			
		// 	var mods = (System.Collections.IEnumerable)typeModManager.GetProperty("Mods").GetValue(modManager);
		// 	foreach(var pmod in mods) {
		// 		var meta = typeModData.GetField("Meta").GetValue(pmod.GetType().GetProperty("Value").GetValue(pmod));
		// 		var metaType = meta.GetType();
				
		// 		if((string)metaType.GetProperty("Name").GetValue(meta) != mod.Name)
		// 			continue;
				
				
		// 	}
		// }
	}
}