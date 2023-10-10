using System;
using Godot;
using LegendOfTheBrave.scripts;

public partial class Door : Node2D {
	[Export] private string next;

	private void OnArea2DBodyEntered(Node2D body) {
		if (body.GetType() != typeof(Player)) return;
		var err = GetTree().ChangeSceneToPacked(Globals.Scenes.GetByName(next));
		if (err != Error.Ok) {
			throw new Exception(err.ToString());
		}
	}
}