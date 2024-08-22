using System;
using System.Collections.Generic;
using Godot;
using LegendOfTheBrave.scripts;
using LegendOfTheBrave.scripts.classes;

public enum PlayerState {
	Idle,
	Running,
	Jump,
	Falling,
	Landing,
	WallSliding,
	WallJump,
	AttackTypeOne,
	AttackTypeTwo,
	AttackTypeThree
}

class TicksTmp : BaseTmp {
	private readonly Player _player;
	private readonly RayCast2D _headFrontChecker;
	private readonly RayCast2D _footFrontChecker;

	public TicksTmp(Player obj, RayCast2D hc, RayCast2D fc) {
		_player = obj;
		_headFrontChecker = hc;
		_footFrontChecker = fc;
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
			_canSlidingWall ??= IsOnWall && _footFrontChecker.IsColliding() && _headFrontChecker.IsColliding();
			return _canSlidingWall.Value;
		}
	}

	private bool? _attackParsed;

	public bool AttackParsed() {
		_attackParsed ??= Input.IsActionJustPressed("attack");
		return _attackParsed.Value;
	}
}

public partial class Player : CharacterBody2D, IStateMachineOwner<PlayerState> {
	private static readonly List<PlayerState> OnFloorStates = new() {
		PlayerState.Idle, PlayerState.Running,
		PlayerState.Landing, PlayerState.AttackTypeOne,
		PlayerState.AttackTypeTwo, PlayerState.AttackTypeThree
	};

	[Export] private int RunSpeed = 180;
	[Export] private int JumpInitYSpeed = -370;
	[Export] private int SlideSpeed = 70;
	private int MaxFallingSpeed = 600;
	[Export] private Vector2 WallJumpInitVelocity = new(200, -370);
	[Export] private bool CanCombo;
	[Export] private int Damage;

	private AnimationPlayer _animationPlayer;
	private Node2D _graphics;
	private HurtBox _hurtBox;

	private StateMachine<PlayerState> _stateMachine;
	private TicksTmp _tmp;


	private ulong? _landingBeginAt;
	private bool _isComboRequested;
	private float _lastWallNormalX;

	public int CurrentDamage => Damage;

	private void _InitCamera() {
		var tileMap = GetTree().Root.GetNodeOrNull<TileMapLayer>("Root/TileMap");
		if (tileMap == null) return;

		var camera2D = GetNode<Camera2D>("Camera2D");

		var rect = tileMap.GetUsedRect().Grow(-1);
		var size = tileMap.TileSet.TileSize;

		camera2D.LimitTop = rect.Position.Y * size.Y;
		camera2D.LimitBottom = rect.End.Y * size.Y;
		camera2D.LimitLeft = rect.Position.X * size.X;
		camera2D.LimitRight = rect.End.X * size.Y;
	}

	public override void _Ready() {
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_graphics = GetNode<Node2D>("Graphics");
		_hurtBox = _graphics.GetNode<HurtBox>("HurtBox");
		_hurtBox.Hurt += OnHurt;

		_InitCamera();

		_tmp = new TicksTmp(
			this,
			_graphics.GetNode<RayCast2D>("HeadFrontChecker"),
			_graphics.GetNode<RayCast2D>("FootFrontChecker")
		);
		_stateMachine = new StateMachine<PlayerState>(this, true);

		OnStateChange(PlayerState.Idle, PlayerState.Idle);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (_stateMachine.Current == PlayerState.Jump && @event.IsActionReleased("jump")) {
			var tmpv = Velocity;
			tmpv.Y = 0;
			Velocity = tmpv;
		}

		if (@event.IsActionPressed("jump") && _landingBeginAt.HasValue) {
			_landingBeginAt = null;
		}

		if (@event.IsActionPressed("attack") && CanCombo) {
			_isComboRequested = true;
		}
	}

	public override void _PhysicsProcess(double delta) {
		_tmp.Clear();
		_stateMachine._PhysicsProcess(delta);
	}

	private void OnHurt(HitBox from) {
		GD.Print($"Player.OnHurt: {from.Owner.Name}");
	}

	public PlayerState GetNextState(PlayerState current) {
		if (!Mathf.IsZeroApprox(Velocity.Y) && OnFloorStates.Contains(current) && !_tmp.IsOnFloor) {
			if (Velocity.Y >= 100) {
				return PlayerState.Falling;
			}

			if (Velocity.Y < -50) {
				return PlayerState.Jump;
			}
		}

		switch (current) {
			case PlayerState.Idle: {
				if ((_tmp.IsOnFloor && _tmp.JumpPressed) || Velocity.Y < 0) {
					return PlayerState.Jump;
				}

				if (!_tmp.IsStill) {
					return PlayerState.Running;
				}

				if (_tmp.IsOnFloor && _tmp.AttackParsed()) {
					return PlayerState.AttackTypeOne;
				}

				break;
			}
			case PlayerState.Running: {
				if ((_tmp.IsOnFloor && _tmp.JumpPressed) || Velocity.Y < 0) {
					return PlayerState.Jump;
				}

				if (_stateMachine.FrameCount > 0 && _tmp.IsOnFloor && _tmp.AttackParsed()) {
					return PlayerState.AttackTypeOne;
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
				if (_tmp.IsOnFloor && _tmp.AttackParsed()) {
					return PlayerState.AttackTypeOne;
				}

				if (_landingBeginAt == null || !_animationPlayer.IsPlaying()) {
					_landingBeginAt = null;
					return PlayerState.Idle;
				}

				break;
			}
			case PlayerState.WallSliding: {
				if (Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Idle;
				}

				if (_stateMachine.FrameCount == 0) {
					_lastWallNormalX = GetWallNormal().X;
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
					if (Velocity.Y > 50) {
						return PlayerState.Falling;
					}
				}
				else {
					_lastWallNormalX = GetWallNormal().X;
				}

				if (_tmp.IsOnFloor && Mathf.IsZeroApprox(Velocity.Y)) {
					return PlayerState.Idle;
				}

				break;
			}
			case PlayerState.AttackTypeOne: {
				if ((_tmp.IsOnFloor && _tmp.JumpPressed) || Velocity.Y < 0) {
					return PlayerState.Jump;
				}

				if (!_tmp.ZeroDirection) {
					return PlayerState.Running;
				}

				if (!_animationPlayer.IsPlaying()) {
					return _isComboRequested ? PlayerState.AttackTypeTwo : PlayerState.Idle;
				}

				break;
			}
			case PlayerState.AttackTypeTwo: {
				if ((_tmp.IsOnFloor && _tmp.JumpPressed) || Velocity.Y < 0) {
					return PlayerState.Jump;
				}

				if (!_tmp.ZeroDirection) {
					return PlayerState.Running;
				}

				if (!_animationPlayer.IsPlaying()) {
					return _isComboRequested ? PlayerState.AttackTypeThree : PlayerState.Idle;
				}

				break;
			}
			case PlayerState.AttackTypeThree: {
				if ((_tmp.IsOnFloor && _tmp.JumpPressed) || Velocity.Y < 0) {
					return PlayerState.Jump;
				}

				if (!_tmp.ZeroDirection) {
					return PlayerState.Running;
				}

				if (!_animationPlayer.IsPlaying()) {
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
			case PlayerState.AttackTypeOne: {
				_isComboRequested = false;
				_animationPlayer.Play("attack_type_1");
				break;
			}
			case PlayerState.AttackTypeTwo: {
				_isComboRequested = false;
				_animationPlayer.Play("attack_type_2");
				break;
			}
			case PlayerState.AttackTypeThree: {
				_isComboRequested = false;
				_animationPlayer.Play("attack_type_3");
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
				_NormalMove(ref tmpv, delta);
				break;
			}
			case PlayerState.Running: {
				_NormalMove(ref tmpv, delta);
				break;
			}
			case PlayerState.Jump: {
				_NormalMove(ref tmpv, delta);
				break;
			}
			case PlayerState.Falling: {
				_NormalMove(ref tmpv, delta);
				break;
			}
			case PlayerState.Landing: {
				_NormalMove(ref tmpv, delta);
				break;
			}
			case PlayerState.WallSliding: {
				_WallSlideMove(ref tmpv, delta);
				break;
			}
			case PlayerState.WallJump: {
				_WallJumpMove(ref tmpv, delta);
				break;
			}
			case PlayerState.AttackTypeOne:
			case PlayerState.AttackTypeTwo:
			case PlayerState.AttackTypeThree: {
				_NormalMove(ref tmpv, delta);
				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(current), current, null);
			}
		}

		Velocity = tmpv;
		MoveAndSlide();
	}

	private void _NormalMove(ref Vector2 tmpv, double delta) {
		var direction = _tmp.Direction;
		tmpv.Y += (float)(Globals.Gravity * delta);
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

	private void _WallSlideMove(ref Vector2 tmpv, double delta) {
		var direction = _tmp.Direction;
		tmpv.X = direction * RunSpeed;
		tmpv.Y = SlideSpeed;
		tmpv.Y += (float)(Globals.Gravity * delta / 3);

		var tmps = _graphics.Scale;
		tmps.X = _lastWallNormalX;
		_graphics.Scale = tmps;
	}

	private void _WallJumpMove(ref Vector2 tmpv, double delta) {
		var tmps = _graphics.Scale;

		if (_stateMachine.Duration < 80) {
			tmpv.X *= _lastWallNormalX;
			tmps.X = _lastWallNormalX;
		}
		else {
			var direction = _tmp.Direction;
			tmpv.X = direction * RunSpeed;
			tmps.X = direction < 0 ? -1 : 1;
		}

		tmpv.Y += (float)(Globals.Gravity * delta);
		_graphics.Scale = tmps;
	}
}