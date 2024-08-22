using System;
using Godot;
using LegendOfTheBrave.scripts;

public partial class Door : Node2D {
	[Export] private string _next;
	private static readonly Type PlayerType = typeof(Player);

	private void OnArea2DBodyEntered(Node2D body) {
		if (body.GetType() != PlayerType) return;
		var err = GetTree().ChangeSceneToPacked(Globals.Scenes.GetByName(_next));
		if (err != Error.Ok) {
			throw new Exception(err.ToString());
		}
	}
}