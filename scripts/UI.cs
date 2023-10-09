using Godot;
using LegendOfTheBrave.scripts;

public partial class UI : CanvasLayer {
	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionReleased("paused")) {
			_on_menu_button_pressed();
			@event.SetMeta(Globals.PausedEventHandledMetaKey, true);
		}
	}

	private void _on_menu_button_pressed() {
		GetTree().Paused = false;
		Hide();
	}
}