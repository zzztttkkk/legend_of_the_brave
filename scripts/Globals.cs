using Godot;
using LegendOfTheBrave.scripts.classes;

namespace LegendOfTheBrave.scripts;

public static class Globals {
	// Names
	public const string PausedEventHandledMetaKey = "HANDLED";

	// Floats
	public static readonly float Gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

	// Config
	public static readonly Config Config = new();
}