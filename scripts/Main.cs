using System;
using System.Collections.Generic;
using Godot;
using LegendOfTheBrave.scripts;

public partial class Main : Node2D {
	private readonly List<Control> _controls = new();
	private Control _currentControl;

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

		_controls.Add(GetNode<Button>("CenterContainer/VBoxContainer/StartButton"));
		_controls.Add(GetNode<Button>("CenterContainer/VBoxContainer/QuitButton"));
	}

	private void MoveFocus(bool down) {
		if (_currentControl == null) {
			_currentControl = _controls[0];
			_currentControl.GrabFocus();
			return;
		}

		var idx = _controls.IndexOf(_currentControl);
		if (down) {
			idx++;
		}
		else {
			idx--;
		}

		Control tmp;
		if (idx <= -1) {
			tmp = _controls[^1];
		}
		else if (idx >= _controls.Count) {
			tmp = _controls[0];
		}
		else {
			tmp = _controls[idx];
		}

		_currentControl.ReleaseFocus();
		tmp.GrabFocus();
		_currentControl = tmp;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.GetType() == typeof(InputEventJoypadMotion)) {
			return;
		}

		if (@event.IsActionPressed("ui_down")) {
			MoveFocus(true);
			return;
		}

		if (@event.IsActionPressed("ui_up")) {
			MoveFocus(false);
			return;
		}

		var is_accept = @event.IsActionPressed("ui_accept");
		if (!is_accept) {
			if (@event.GetType() == typeof(InputEventJoypadButton)) {
				var evt = (InputEventJoypadButton)@event;
				is_accept = evt.ButtonIndex == JoyButton.A;
			}
		}


		if (!is_accept) return;
		if (_currentControl == null) return;
		if (_currentControl.GetType() != typeof(Button)) return;

		var btn = (Button)_currentControl;
		btn.EmitSignal(BaseButton.SignalName.Pressed);
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