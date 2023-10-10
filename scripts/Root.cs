using Godot;
using LegendOfTheBrave.scripts;

public partial class Root : Node2D {
	private CanvasLayer _ui;

	public override void _Ready() {
		_ui = GetNodeOrNull<CanvasLayer>("uis/ui");
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionReleased("paused")) {
			if (_ui == null) return;

			if (@event.GetMeta(Globals.PausedEventHandledMetaKey, @default: false).AsBool()) {
				return;
			}

			GetTree().Paused = true;
			_ui.Show();
		}
	}
}