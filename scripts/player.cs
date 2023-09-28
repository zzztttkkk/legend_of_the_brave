using System;
using Godot;
using LegendOfTheBrave.scripts.classes;

public enum PlayerState {
	Idle,
	Running,
	Jump,
	Falling,
	Landing,
	WallSliding,
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

	private bool? _jumpPressed;

	public bool JumpPressed {
		get {
			_jumpPressed ??= Input.IsActionJustPressed("jump");
			return _jumpPressed.Value;
		}
	}

	private bool? _isOnWall;

	public bool IsOnWall {
		get {
			_isOnWall ??= _player.IsOnWall();
			return _isOnWall.Value;
		}
	}
}

public partial class player : CharacterBody2D, IStateMachineOwner<PlayerState> {
	private static readonly float G = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

	[Export] private int RunSpeed = 180;
	[Export] private int JumpSpeed = -370;
	[Export] private int SlideSpeed = 70;
	[Export] private int MaxFallingSpeed = 600;

	private AnimationPlayer _animationPlayer;
	private Node2D _graphics;

	private StateMachine<PlayerState> _stateMachine;
	private TicksTmp _tmp;

	private ulong? _landingBeginAt;
	private bool _turnFaceInSliding;


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
				if ((_tmp.IsOnFloor && _tmp.JumpPressed) || Velocity.Y < 0) {
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
				if ((_tmp.IsOnFloor && _tmp.JumpPressed) || Velocity.Y < 0) {
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

				if (_tmp.IsOnWall && Math.Abs(Velocity.Y) > SlideSpeed) {
					return PlayerState.WallSliding;
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
			case PlayerState.WallSliding: {
				if (Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Landing;
				}

				if (!_tmp.IsOnWall) {
					return PlayerState.Falling;
				}

				if (_tmp.JumpPressed) {
					return PlayerState.Jump;
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
			case PlayerState.WallSliding: {
				if (_turnFaceInSliding) {
					_turnFaceInSliding = false;
				}

				_animationPlayer.Play("wall_sliding");
				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(to), to, null);
			}
		}
	}

	public void TickPhysics(PlayerState current, double delta) {
		var tmpv = Velocity;

		switch (current) {
			case PlayerState.Idle: {
				move(ref tmpv, delta);
				break;
			}
			case PlayerState.Running: {
				move(ref tmpv, delta);
				break;
			}
			case PlayerState.Jump: {
				move(ref tmpv, delta);
				break;
			}
			case PlayerState.Falling: {
				move(ref tmpv, delta);
				break;
			}
			case PlayerState.Landing: {
				move(ref tmpv, delta);
				break;
			}
			case PlayerState.WallSliding: {
				wallSliding(ref tmpv);
				if (!_turnFaceInSliding) {
					turnFace();
					_turnFaceInSliding = true;
				}

				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(current), current, null);
			}
		}

		Velocity = tmpv;
		MoveAndSlide();
	}

	private void move(ref Vector2 tmpv, double delta) {
		var direction = _tmp.Direction;
		tmpv.Y += (float)(G * delta);
		tmpv.X = direction * RunSpeed;

		// 达到最大下降速度后，不能左右移动
		if (tmpv.Y >= MaxFallingSpeed) {
			tmpv.Y = MaxFallingSpeed;
			tmpv.X = 0;
		}

		if (_tmp.ZeroDirection) return;

		var tmps = _graphics.Scale;
		tmps.X = direction < 0 ? -1 : 1;
		_graphics.Scale = tmps;
	}


	private void turnFace() {
		var tmps = _graphics.Scale;
		tmps.X *= -1;
		_graphics.Scale = tmps;
	}

	private void wallSliding(ref Vector2 tmpv) {
		var direction = _tmp.Direction;
		tmpv.X = direction * RunSpeed;
		tmpv.Y = SlideSpeed;
	}
}