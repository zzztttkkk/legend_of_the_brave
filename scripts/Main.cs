using System;
using Godot;
using LegendOfTheBrave.scripts;

public partial class Main : Node2D {
	private void _CustomWindows() {
		var cwsize = Globals.Config.GetVector2I("dev.window_size");
		if (cwsize.HasValue) {
			GetWindow().Size = cwsize.Value;
		}

		var cwpos = Globals.Config.GetVector2I("dev.window_pos");
		if (cwpos.HasValue) {
			GetWindow().Position = cwpos.Value;
		}
	}

	public override void _Ready() {
		_CustomWindows();
	}

	private void OnStartBtnPressed() {
		var err = GetTree().ChangeSceneToPacked(Globals.Scenes.First);
		if (err != Error.Ok) {
			throw new Exception(err.ToString());
		}
	}

	private void OnQuitBtnPressed() {
		GetTree().Quit();
	}
}