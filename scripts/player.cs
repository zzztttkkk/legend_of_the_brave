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
	WallJump,
}

class TicksTmp : BaseTmp {
	private readonly player _player;
	private readonly RayCast2D _handChecker;
	private readonly RayCast2D _footChecker;

	public TicksTmp(player obj, RayCast2D hc, RayCast2D fc) {
		_player = obj;
		_handChecker = hc;
		_footChecker = fc;
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

	private bool? _canSlidingWall;

	public bool CanSlidingWall {
		get {
			_canSlidingWall ??= IsOnWall && _footChecker.IsColliding() && _handChecker.IsColliding();
			return _canSlidingWall.Value;
		}
	}

	private float? _wallNormalX;

	public float WallNormalX {
		get {
			_wallNormalX ??= _player.GetWallNormal().X;
			return _wallNormalX.Value;
		}
	}
}

public partial class player : CharacterBody2D, IStateMachineOwner<PlayerState> {
	private static readonly float G = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

	[Export] private int RunSpeed = 180;
	[Export] private int JumpInitYSpeed = -370;
	[Export] private int SlideSpeed = 70;
	[Export] private int MaxFallingSpeed = 600;
	[Export] private Vector2 WallJumpInitVelocity = new(240, -370);

	private AnimationPlayer _animationPlayer;
	private Node2D _graphics;

	private StateMachine<PlayerState> _stateMachine;
	private TicksTmp _tmp;


	private ulong? _landingBeginAt;
	private ulong _LandingAnimationLength;

	public override void _Ready() {
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_graphics = GetNode<Node2D>("Graphics");

		_LandingAnimationLength = (ulong)(_animationPlayer.GetAnimation("landing").Length * 1000);

		_tmp = new TicksTmp(
			this,
			_graphics.GetNode<RayCast2D>("HandChecker"),
			_graphics.GetNode<RayCast2D>("FootChecker")
		);
		_stateMachine = new StateMachine<PlayerState>(this);

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
				if (_stateMachine.FrameCount > 0) {
					if (Velocity.Y > 50) {
						return PlayerState.Falling;
					}

					if (_tmp.IsOnFloor && Mathf.IsZeroApprox(Velocity.Y)) {
						return PlayerState.Idle;
					}
				}

				break;
			}
			case PlayerState.Falling: {
				if (Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Landing;
				}

				if (_tmp.CanSlidingWall) {
					return PlayerState.WallSliding;
				}

				break;
			}
			case PlayerState.Landing: {
				if (_landingBeginAt == null ||
				    Time.GetTicksMsec() - _landingBeginAt.Value >= _LandingAnimationLength) {
					_landingBeginAt = null;
					return PlayerState.Idle;
				}

				break;
			}
			case PlayerState.WallSliding: {
				if (Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Idle;
				}

				if (_stateMachine.FrameCount > 0 && _tmp.JumpPressed) {
					return PlayerState.WallJump;
				}

				// 即使松开方向键，也能滑墙至少20帧，给玩家爬墙跳换方向的时间。
				if (_stateMachine.FrameCount > 20 && !_tmp.IsOnWall) {
					return PlayerState.Falling;
				}

				break;
			}
			case PlayerState.WallJump: {
				if (_stateMachine.FrameCount > 0) {
					if (_tmp.CanSlidingWall) {
						return PlayerState.WallSliding;
					}

					if (Velocity.Y > 50) {
						return PlayerState.Falling;
					}
				}

				if (_tmp.IsOnFloor && Mathf.IsZeroApprox(Velocity.Y)) {
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
			case PlayerState.WallJump: {
				if (_stateMachine.FrameCount == 0) {
					Velocity = WallJumpInitVelocity;
				}

				_animationPlayer.Play("jump");
				break;
			}
			case PlayerState.Jump: {
				if (_stateMachine.FrameCount == 0) {
					var tmpv = Velocity;
					tmpv.Y = JumpInitYSpeed;
					Velocity = tmpv;
				}

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
				wallSlide(ref tmpv, delta);
				break;
			}
			case PlayerState.WallJump: {
				wallJump(ref tmpv, delta);
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

	private void wallSlide(ref Vector2 tmpv, double delta) {
		var direction = _tmp.Direction;
		tmpv.X = direction * RunSpeed;
		tmpv.Y = SlideSpeed;
		tmpv.Y += (float)(G * delta / 3);

		var tmps = _graphics.Scale;
		tmps.X = _tmp.WallNormalX;
		_graphics.Scale = tmps;
	}


	private void wallJump(ref Vector2 tmpv, double delta) {
		if (_stateMachine.FrameCount < 8) {
			tmpv.X *= _tmp.WallNormalX;
		}
		else {
			var direction = _tmp.Direction;
			tmpv.X = direction * RunSpeed;
		}

		tmpv.Y += (float)(G * delta);

		var tmps = _graphics.Scale;
		tmps.X = _tmp.WallNormalX;
		_graphics.Scale = tmps;
	}
}