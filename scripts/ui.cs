using Godot;
using LegendOfTheBrave.scripts;

public partial class ui : CanvasLayer {
	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionReleased("paused")) {
			_on_menu_button_pressed();
			@event.SetMeta(Names.PausedEventHandledMetaKey, true);
		}
	}

	private void _on_menu_button_pressed() {
		GetTree().Paused = false;
		Hide();
	}
}