using Godot;
using LegendOfTheBrave.scripts.classes;

public enum FaceDirection {
	Left = -1,
	Right = 1,
}

public partial class Enemy : CharacterBody2D {
	[Export] protected FaceDirection _faceDirection = FaceDirection.Left;
	[Export] protected int _speed = 200;
	[Export] protected int _hp = 1000;

	protected Node2D _graphics;
	protected AnimationPlayer _animationPlayer;
	protected HurtBox _hurtBox;

	public FaceDirection FaceDirection {
		get => _faceDirection;

		set {
			_faceDirection = value;

			if (_graphics == null) {
				Ready += turnFaceByValue;
			}
			else {
				turnFaceByValue();
			}
		}
	}

	private void turnFaceByValue() {
		var tmps = _graphics.Scale;
		tmps.X = _faceDirection == FaceDirection.Left ? 1 : -1;
		_graphics.Scale = tmps;
	}

	protected void TurnFace() {
		FaceDirection = _faceDirection == FaceDirection.Left ? FaceDirection.Right : FaceDirection.Left;
	}

	public override void _Ready() {
		_graphics = GetNode<Node2D>("Graphics");
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_hurtBox = _graphics.GetNode<HurtBox>("HurtBox");
		_hurtBox.Hurt += OnHurt;
	}

	protected virtual void OnHurt(HitBox from) {
	}
}