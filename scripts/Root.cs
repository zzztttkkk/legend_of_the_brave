using Godot;
using LegendOfTheBrave.scripts;

public partial class Root : Node2D {
	private CanvasLayer _ui;

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