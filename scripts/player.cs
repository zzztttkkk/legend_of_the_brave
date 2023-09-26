using System;
using Godot;

public partial class player : CharacterBody2D {
	private static readonly float G = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

	[Export] private int RunSpeed = 200;
	[Export] private int JumpSpeed = -400;

	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite2D;

	public override void _Ready() {
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _PhysicsProcess(double delta) {
		var isOnFloor = IsOnFloor();

		var tmpv = Velocity;

		var direction = Input.GetAxis("move_left", "move_right");
		tmpv.X = direction * RunSpeed;
		if (tmpv.Y >= 500) tmpv.X = 0;

		tmpv.Y += (float)(delta * G);

		if (isOnFloor && Input.IsActionJustPressed("jump")) {
			tmpv.Y += JumpSpeed;
		}

		if (isOnFloor) {
			_animationPlayer.Play(Mathf.IsZeroApprox(direction) ? "idle" : "running");
		}
		else {
			_animationPlayer.Play("jump");
		}

		if (!Mathf.IsZeroApprox(direction)) {
			_sprite2D.FlipH = direction < 0;
		}

		tmpv.Y = Math.Min(tmpv.Y, 600);

		Velocity = tmpv;
		MoveAndSlide();
	}
}