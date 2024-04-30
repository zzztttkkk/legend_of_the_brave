using Godot;
using LegendOfTheBrave.scripts.classes;

public enum FaceDirection {
	Left = -1,
	Right = 1,
}

public partial class Enemy : CharacterBody2D {
	[Export] protected FaceDirection FaceDirectionValue = FaceDirection.Left;
	[Export] protected int SpeedValue = 200;
	[Export] protected int HpValue = 1000;

	protected Node2D Graphics;
	protected AnimationPlayer AnimationPlayer;
	protected HurtBox HurtBox;

	public FaceDirection FaceDirection {
		get => FaceDirectionValue;

		set {
			FaceDirectionValue = value;

			if (Graphics == null) {
				Ready += _TurnFaceByValue;
			}
			else {
				_TurnFaceByValue();
			}
		}
	}

	private void _TurnFaceByValue() {
		var tmps = Graphics.Scale;
		tmps.X = FaceDirectionValue == FaceDirection.Left ? 1 : -1;
		Graphics.Scale = tmps;
	}

	protected void TurnFace() {
		FaceDirection = FaceDirectionValue == FaceDirection.Left ? FaceDirection.Right : FaceDirection.Left;
	}

	public override void _Ready() {
		Graphics = GetNode<Node2D>("Graphics");
		AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		HurtBox = Graphics.GetNode<HurtBox>("HurtBox");
		HurtBox.Hurt += OnHurt;

		_TurnFaceByValue();
	}

	protected virtual void OnHurt(HitBox from) { }
}