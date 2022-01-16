using System;
using System.Numerics;

using ImGuiNET;

namespace Aetherment.GUI {
	internal partial class UI : IDisposable {
		private bool shouldDraw = false;
		
		public UI() {
			Show();
			
			Aetherment.Interface.UiBuilder.OpenConfigUi += Show;
			Aetherment.Interface.UiBuilder.Draw += Draw;
		}
		
		public void Dispose() {
			Aetherment.Interface.UiBuilder.OpenConfigUi -= Show;
			Aetherment.Interface.UiBuilder.Draw -= Draw;
		}
		
		public void Show() {
			shouldDraw = true;
		}
		
		private void Draw() {
			if(!shouldDraw)
				return;
			
			ImGui.SetNextWindowSize(new Vector2(1080, 720));
			ImGui.Begin("Aetherment", ref shouldDraw);
			// stuff
			ImGui.End();
		}
	}
}