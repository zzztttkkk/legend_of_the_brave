using Godot;

namespace LegendOfTheBrave.scripts.classes;

public partial class HitBox : Area2D {
	[Signal]
	public delegate void HitEventHandler(HurtBox to);

	HitBox() {
		AreaEntered += delegate(Area2D area) {
			var hurtBox = (HurtBox)(area);
			EmitSignal(SignalName.Hit, hurtBox);
			hurtBox.EmitSignal(HurtBox.SignalName.Hurt, this);
		};
	}
}