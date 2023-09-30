using Godot;

namespace LegendOfTheBrave.scripts;

public static class Globals {
	// Names
	public static readonly string PausedEventHandledMetaKey = "HANDLED";

	// Floats
	public static readonly float Gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
}