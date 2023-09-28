using System;
using Godot;
using LegendOfTheBrave.scripts.classes;

public enum PlayerState {
	Idle,
	Running,
	Jump,
	Falling,
	Landing,
}

class TicksTmp : BaseTmp {
	private readonly player _player;

	public TicksTmp(player obj) {
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
	[Export] private int JumpSpeed = -370;

	private AnimationPlayer _animationPlayer;
	private Node2D _graphics;

	private StateMachine<PlayerState> _stateMachine;
	private TicksTmp _tmp;

	private ulong? _landingBeginAt;


	public override void _Ready() {
		_tmp = new TicksTmp(this);
		_stateMachine = new StateMachine<PlayerState>(this);

		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_graphics = GetNode<Node2D>("Graphics");

		OnStateChange(PlayerState.Idle, PlayerState.Idle);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionReleased("jump")) {
			var tmpv = Velocity;
			if (!(tmpv.Y < -100)) return;
			tmpv.Y = -100;
			Velocity = tmpv;
		}

		if (@event.IsActionPressed("jump") && _landingBeginAt.HasValue) {
			_landingBeginAt = null;
		}
	}

	public override void _PhysicsProcess(double delta) {
		_tmp.Clear();
		_stateMachine._PhysicsProcess(delta);
	}

	public PlayerState GetNextState(PlayerState current) {
		switch (current) {
			case PlayerState.Idle: {
				if (_tmp.ShouldJump || Velocity.Y < 0) {
					return PlayerState.Jump;
				}

				if (Velocity.Y > 0) {
					return PlayerState.Falling;
				}

				if (!_tmp.IsStill) {
					return PlayerState.Running;
				}

				break;
			}
			case PlayerState.Running: {
				if (_tmp.ShouldJump || Velocity.Y < 0) {
					return PlayerState.Jump;
				}

				if (Velocity.Y > 0) {
					return PlayerState.Falling;
				}

				if (_tmp.IsStill) {
					return PlayerState.Idle;
				}

				break;
			}
			case PlayerState.Jump: {
				if (Velocity.Y > 50) {
					return PlayerState.Falling;
				}

				if (_tmp.IsOnFloor && Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Idle;
				}

				break;
			}
			case PlayerState.Falling: {
				if (Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Landing;
				}

				break;
			}
			case PlayerState.Landing: {
				if (_landingBeginAt == null || Time.GetTicksMsec() - _landingBeginAt.Value >= 300) {
					_landingBeginAt = null;
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
			case PlayerState.Falling: {
				_animationPlayer.Play("falling");
				break;
			}
			case PlayerState.Landing: {
				_landingBeginAt ??= Time.GetTicksMsec();
				_animationPlayer.Play("landing");
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
			case PlayerState.Falling: {
				break;
			}
			case PlayerState.Landing: {
				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(current), current, null);
			}
		}

		move(delta);
	}

	private void move(double delta) {
		var direction = _tmp.Direction;
		var tmpv = Velocity;
		tmpv.Y += (float)(G * delta);
		tmpv.X = direction * RunSpeed;
		Velocity = tmpv;
		if (!_tmp.ZeroDirection) {
			var tmps = _graphics.Scale;
			tmps.X = direction < 0 ? -1 : 1;
			_graphics.Scale = tmps;
		}

		MoveAndSlide();
	}
}