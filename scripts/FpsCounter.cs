using Godot;

public partial class FpsCounter : Label {
	public override void _Process(double delta) {
		Text = $"Fps: {Engine.GetFramesPerSecond()}";
	}
}