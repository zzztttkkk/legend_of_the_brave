using Godot;

public enum FaceDirection {
	Left = -1,
	Right = 1,
}

public partial class Enemy : CharacterBody2D {
	[Export] protected FaceDirection _faceDirection = FaceDirection.Left;
	[Export] protected int _speed = 200;

	protected Node2D _graphics;
	protected AnimationPlayer _animationPlayer;

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
	}
}