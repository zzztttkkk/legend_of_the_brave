using System;
using Godot;

public enum PlayerState {
	Idle,
	Running,
	Jump,
}

class TicksSharedData : BaseTicksSharedData {
	private readonly player _player;

	public TicksSharedData(player obj) {
		_player = obj;
	}

	private bool? _isOnFloor;

	public bool IsOnFloor {
		get {
			_isOnFloor ??= _player.IsOnFloor();
			return _isOnFloor.Value;
		}
	}

	private float? _direction;

	public float Direction {
		get {
			_direction ??= Input.GetAxis("move_left", "move_right");
			return _direction.Value;
		}
	}

	private bool? _zeroDirection;

	public bool ZeroDirection {
		get {
			_zeroDirection ??= Mathf.IsZeroApprox(Direction);
			return _zeroDirection.Value;
		}
	}

	private bool? _isStill;

	public bool IsStill {
		get {
			_isStill ??= ZeroDirection && Mathf.IsZeroApprox(_player.Velocity.X);
			return _isStill.Value;
		}
	}

	private bool? _shouldJump;

	public bool ShouldJump {
		get {
			_shouldJump ??= IsOnFloor && Input.IsActionJustPressed("jump");
			return _shouldJump.Value;
		}
	}
}

public partial class player : CharacterBody2D, IStateMachineOwner<PlayerState> {
	private static readonly float G = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

	[Export] private int RunSpeed = 180;
	[Export] private int JumpSpeed = -380;

	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite2D;
	private StateMachine<PlayerState> _stateMachine;
	private TicksSharedData _sharedData;

	public override void _Ready() {
		_sharedData = new TicksSharedData(this);
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_stateMachine = new StateMachine<PlayerState>(this);
		OnStateChange(PlayerState.Idle, PlayerState.Idle);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (!@event.IsActionReleased("jump")) return;

		var tmpv = Velocity;
		if (!(tmpv.Y < -100)) return;
		tmpv.Y = -100;
		Velocity = tmpv;
	}

	public override void _PhysicsProcess(double delta) {
		_sharedData.Clear();
		_stateMachine._PhysicsProcess(delta);
	}

	public PlayerState GetNextState(PlayerState current) {
		var isOnFloor = _sharedData.IsOnFloor;
		var isStill = _sharedData.IsStill;
		var shouldJump = _sharedData.ShouldJump;

		switch (current) {
			case PlayerState.Idle: {
				if (!isStill) {
					return PlayerState.Running;
				}

				if (shouldJump) {
					return PlayerState.Jump;
				}

				break;
			}
			case PlayerState.Running: {
				if (isStill) {
					return PlayerState.Idle;
				}

				if (shouldJump) {
					return PlayerState.Jump;
				}

				break;
			}
			case PlayerState.Jump: {
				if (isOnFloor && Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Idle;
				}

				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(current), current, null);
			}
		}

		return current;
	}

	public void OnStateChange(PlayerState from, PlayerState to) {
		if (from == to && to != PlayerState.Idle) return;

		switch (to) {
			case PlayerState.Idle: {
				_animationPlayer.Play("idle");
				break;
			}
			case PlayerState.Running: {
				_animationPlayer.Play("running");
				break;
			}
			case PlayerState.Jump: {
				var tmpv = Velocity;
				tmpv.Y = JumpSpeed;
				Velocity = tmpv;
				_animationPlayer.Play("jump");
				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(to), to, null);
			}
		}
	}

	public void TickPhysics(PlayerState current, double delta) {
		switch (current) {
			case PlayerState.Idle: {
				break;
			}
			case PlayerState.Running: {
				break;
			}
			case PlayerState.Jump: {
				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(current), current, null);
			}
		}

		move(delta);
	}

	private void move(double delta) {
		var direction = _sharedData.Direction;
		var tmpv = Velocity;
		tmpv.Y += (float)(G * delta);
		tmpv.X = direction * RunSpeed;
		Velocity = tmpv;
		if (!_sharedData.ZeroDirection) {
			_sprite2D.FlipH = direction < 0;
		}

		MoveAndSlide();
	}
}