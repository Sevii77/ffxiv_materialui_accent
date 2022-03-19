using ImGuiNET;
using Dalamud.Logging;

namespace Aetherment.GUI {
	internal partial class UI {
		private void DrawSettings() {
			bool save = false;
			
			ImGui.Text("TODO: make fancy");
			save = ImGui.Checkbox("Auto Update", ref Aetherment.Config.AutoUpdate) || save;
			save = ImGui.Checkbox("Link Options", ref Aetherment.Config.LinkOptions) || save;
			save = ImGui.Checkbox("Advanced Options", ref Aetherment.Config.AdvancedMode) || save;
			
			if(Aetherment.Config.AdvancedMode) {
				save = ImGui.Checkbox("Force RGBA Color Select", ref Aetherment.Config.ForceColor4) || save;
				save = ImGui.Checkbox("Local Mods", ref Aetherment.Config.LocalMods) || save;
				save = ImGui.InputTextWithHint("", "Local Mods Path", ref Aetherment.Config.LocalModsPath, 200) || save;
				save = ImGui.Checkbox("Developer Mode", ref Aetherment.Config.DevMode) || save;
			}
			
			if(save) {
				PluginLog.Log("Save main config");
				Aetherment.SaveConfig();
			}
		}
	}
}