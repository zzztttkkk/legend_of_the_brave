using System.Collections.Generic;
using Godot;
using LegendOfTheBrave.scripts.classes;

namespace LegendOfTheBrave.scripts;

public class Scenes {
	public readonly PackedScene First = ResourceLoader.Load<PackedScene>("res://worlds/first.tscn");
	public readonly PackedScene Second = ResourceLoader.Load<PackedScene>("res://worlds/second.tscn");

	private readonly Dictionary<string, PackedScene> names;

	public Scenes() {
		names = new() {
			["First"] = First,
			["Second"] = Second
		};
	}

	public PackedScene GetByName(string name) {
		return names.TryGetValue(name, out var value) ? value : null;
	}
}

public static class Globals {
	// Names
	public const string PausedEventHandledMetaKey = "HANDLED";

	// Floats
	public static readonly float Gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

	// Config
	public static readonly Config Config = new();

	// Scenes
	public static readonly Scenes Scenes = new();
}