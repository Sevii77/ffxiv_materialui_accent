using Dalamud.Interface.Style;
using Dalamud.Logging;
using ImGuiNET;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System;

namespace MaterialUI {
	public class DalamudStyle {
		public static void Apply(MaterialUI main, Dictionary<string, int> styleOverrides) {
			Config config = main.config;
			Vector4 accent = new Vector4(config.color.X, config.color.Y, config.color.Z, 1);
			Vector4 accentHalf = accent * new Vector4(0.5f, 0.5f, 0.5f, 1);
			
			// make default style
			var style = StyleModelV1.DalamudStandard;
			style.Name = "Material UI";
			style.WindowPadding = new Vector2(10, 6);
			style.FramePadding = new Vector2(6, 2);
			style.CellPadding = new Vector2(4, 4);
			style.ItemSpacing = new Vector2(10, 6);
			style.ItemInnerSpacing = new Vector2(4, 4);
			style.TouchExtraPadding = new Vector2(2, 2);
			style.IndentSpacing = 21;
			style.ScrollbarSize = 11;
			style.GrabMinSize = 17;
			
			style.WindowBorderSize = 1;
			style.ChildBorderSize = 0;
			style.PopupBorderSize = 1;
			style.FrameBorderSize = 0;
			style.TabBorderSize = 0;
			
			style.WindowRounding = 0;
			// style.WindowRounding = 11;
			style.ChildRounding = 0;
			style.FrameRounding = 0;
			// style.FrameRounding = 12;
			style.PopupRounding = 0;
			// style.PopupRounding = 12;
			style.ScrollbarRounding = 12;
			style.GrabRounding = 12;
			style.LogSliderDeadzone = 4;
			style.TabRounding = 0;
			// style.TabRounding = 4;
			
			style.WindowTitleAlign = new Vector2(0, 0.5f);
			style.WindowMenuButtonPosition = ImGuiDir.Right;
			style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
			style.SelectableTextAlign = new Vector2(0, 0);
			style.DisplaySafeAreaPadding = new Vector2(3, 3);
			
			style.Colors = new Dictionary<string, Vector4>
			{
				{"Text", new Vector4(1, 1, 1, 1)},
				{"TextDisabled", new Vector4(0.5019608f, 0.5019608f, 0.5019608f, 1)},
				{"WindowBg", new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 1)},
				{"ChildBg", new Vector4(0, 0, 0, 0)},
				{"PopupBg", new Vector4(0.08955222f, 0.08955222f, 0.08955222f, 1)},
				{"Border", new Vector4(0, 0, 0, 1)},
				{"BorderShadow", new Vector4(0, 0, 0, 0)},
				{"FrameBg", new Vector4(0.16078432f, 0.16078432f, 0.16078432f, 1)},
				{"FrameBgHovered", new Vector4(0.22352941f, 0.22352941f, 0.22352941f, 1)},
				{"FrameBgActive", new Vector4(0.21960784f, 0.21568628f, 0.21960784f, 1)},
				{"TitleBg", new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 1)},
				{"TitleBgActive", new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 1)},
				{"TitleBgCollapsed", new Vector4(0.12941177f, 0.1254902f, 0.12941177f, 1)},
				{"MenuBarBg", new Vector4(0.14f, 0.14f, 0.14f, 1)},
				{"ScrollbarBg", new Vector4(0, 0, 0, 0)},
				{"ScrollbarGrab", new Vector4(0.24313726f, 0.24313726f, 0.24313726f, 1)},
				{"ScrollbarGrabHovered", new Vector4(0.27601808f, 0.2760153f, 0.27601808f, 1)},
				{"ScrollbarGrabActive", new Vector4(0.27450982f, 0.27450982f, 0.27450982f, 1)},
				{"CheckMark", accent},
				{"SliderGrab", new Vector4(0.39800596f, 0.39800596f, 0.39800596f, 1)},
				{"SliderGrabActive", new Vector4(0.4825822f, 0.4825822f, 0.4825822f, 1)},
				{"Button", new Vector4(0, 0, 0, 0)},
				{"ButtonHovered", new Vector4(0.16078432f, 0.16078432f, 0.16078432f, 1)},
				{"ButtonActive", new Vector4(0.22352941f, 0.22352941f, 0.22352941f, 1)},
				{"Header", new Vector4(0.0f, 0.0f, 0.0f, 0.23529412f)},
				{"HeaderHovered", new Vector4(0.0f, 0.0f, 0.0f, 0.3529412f)},
				{"HeaderActive", new Vector4(0.0f, 0.0f, 0.0f, 0.47058824f)},
				{"Separator", new Vector4(0, 0, 0, 0)},
				// {"Separator", new Vector4(0.17254902f, 0.17254902f, 0.17254902f, 1)},
				{"SeparatorHovered", accentHalf},
				{"SeparatorActive", accent},
				{"ResizeGrip", new Vector4(0.0f, 0.0f, 0.0f, 0.0f)},
				{"ResizeGripHovered", new Vector4(0.0f, 0.0f, 0.0f, 0.0f)},
				{"ResizeGripActive", accent},
				{"Tab", new Vector4(0.16078432f, 0.16078432f, 0.16078432f, 1)},
				{"TabHovered", accentHalf},
				{"TabActive", accent},
				{"TabUnfocused", new Vector4(0.16078432f, 0.15294118f, 0.16078432f, 1)},
				{"TabUnfocusedActive", accent},
				{"DockingPreview", accent * new Vector4(1, 1, 1, 0.5f)},
				{"DockingEmptyBg", new Vector4(0.2f, 0.2f, 0.2f, 1)},
				{"PlotLines", new Vector4(0.61f, 0.61f, 0.61f, 1)},
				{"PlotLinesHovered", new Vector4(1, 0.43f, 0.35f, 1)},
				{"PlotHistogram", new Vector4(0.9f, 0.7f, 0, 1)},
				{"PlotHistogramHovered", new Vector4(1, 0.6f, 0, 1)},
				{"TableHeaderBg", new Vector4(0.19f, 0.19f, 0.2f, 1)},
				{"TableBorderStrong", new Vector4(0.31f, 0.31f, 0.35f, 1)},
				{"TableBorderLight", new Vector4(0.23f, 0.23f, 0.25f, 1)},
				{"TableRowBg", new Vector4(0, 0, 0, 0)},
				{"TableRowBgAlt", new Vector4(1, 1, 1, 0.06f)},
				{"TextSelectedBg", accent},
				{"DragDropTarget", accent},
				{"NavHighlight", accent},
				{"NavWindowingHighlight", new Vector4(1, 1, 1, 0.7f)},
				{"NavWindowingDimBg", new Vector4(0.8f, 0.8f, 0.8f, 0.2f)},
				{"ModalWindowDimBg", new Vector4(0.8f, 0.8f, 0.8f, 0.35f)},
			};
			
			style.BuiltInColors = new DalamudColors{
				DalamudRed = new Vector4(1, 0, 0, 1),
				DalamudGrey = new Vector4(0.7f, 0.7f, 0.7f, 1),
				DalamudGrey2 = new Vector4(0.7f, 0.7f, 0.7f, 1),
				DalamudGrey3 = new Vector4(0.5f, 0.5f, 0.5f, 1),
				DalamudWhite = new Vector4(1, 1, 1, 1),
				DalamudWhite2 = new Vector4(0.878f, 0.878f, 0.878f, 1),
				DalamudOrange = new Vector4(1, 0.709f, 0, 1),
				TankBlue = new Vector4(0.12180391f, 0.30259418f, 1.66169155f, 1),
				HealerGreen = new Vector4(0.16052085f, 0.72398186f, 0.2727031f, 1),
				DPSRed = new Vector4(0.88687783f, 0.13644274f, 0.13644274f, 1),
			};
			
			// special edits for penumbra window setting
			try {
				string configPath = $"{main.pluginInterface.ConfigFile.DirectoryName}/Penumbra.json";
				dynamic penumbraData = JsonConvert.DeserializeObject(File.ReadAllText(configPath));
				string collection = (string)penumbraData?.CurrentCollection;
				
				string collectionPath = $"{main.pluginInterface.ConfigFile.DirectoryName}/Penumbra/collections/{collection}.json";
				dynamic collectionData = JsonConvert.DeserializeObject(File.ReadAllText(collectionPath));
				if(collectionData.Settings["Material UI"].Settings["Selected Window"] == 1) {
					style.WindowBorderSize = 2;
					style.PopupBorderSize = 0;
					style.Colors["Border"] = accent;
				}
			} catch(Exception e) {
				PluginLog.LogError(e, "Failed checking penumbra collection");
			}
			
			// apply mod style edits
			var styleType = style.GetType();
			foreach(KeyValuePair<string, int> val in styleOverrides) {
				styleType.GetProperty(val.Key).SetValue(style, val.Value);
			}
			
			style.Apply();
			
			// reflection garbage to save it
			var ass = typeof(Dalamud.ClientLanguage).Assembly;
			var t = ass.GetType("Dalamud.Configuration.Internal.DalamudConfiguration");
			var dalamudConfig = ass.GetType("Dalamud.Service`1").MakeGenericType(t)
				.GetMethod("Get").Invoke(null, BindingFlags.Default, null, new object[] {}, null);
			
			PropertyInfo savedStylesInfo = t.GetProperty("SavedStyles");
			List<StyleModel> savedStyles = (List<StyleModel>)savedStylesInfo.GetValue(dalamudConfig);
			foreach(StyleModel savedStyle in savedStyles)
				if(savedStyle.Name == "Material UI") {
					savedStyles.Remove(savedStyle);
					break;
				}
			savedStyles.Add(style);
			savedStylesInfo.SetValue(dalamudConfig, savedStyles);
			
			PropertyInfo chosenStyleInfo = t.GetProperty("ChosenStyle");
			chosenStyleInfo.SetValue(dalamudConfig, "Material UI");
			
			t.GetMethod("Save").Invoke(dalamudConfig, new object[] {});
		}
	}
}