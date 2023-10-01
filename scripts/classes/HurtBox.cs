using Godot;

namespace LegendOfTheBrave.scripts.classes;

public partial class HurtBox : Area2D {
	[Signal]
	public delegate void HurtEventHandler(HitBox from);
}