using System;
using Godot;

public partial class FPSCounter : Label {
	public override void _Process(double delta) {
		Text = $"FPS: {Math.Round(Engine.GetFramesPerSecond())}";
	}
}