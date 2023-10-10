using Godot;
using System;
using LegendOfTheBrave.scripts;
using LegendOfTheBrave.scripts.classes;

public enum BoarState {
	Idle,
	Walk,
	Run,
	OnHit,
}

class BoarTmp : BaseTmp {
	private readonly RayCast2D _wallChecker;
	private readonly RayCast2D _floorChecker1;
	private readonly RayCast2D _floorChecker2;
	private readonly RayCast2D _playerFaceChecker;

	public BoarTmp(RayCast2D wc, RayCast2D fc1, RayCast2D fc2, RayCast2D pfc) {
		_wallChecker = wc;
		_floorChecker1 = fc1;
		_floorChecker2 = fc2;
		_playerFaceChecker = pfc;
	}

	private bool? _seePlayer;

	public bool SeePlayer {
		get {
			_seePlayer ??= (
				_playerFaceChecker.IsColliding()
				&&
				_playerFaceChecker.GetCollider().GetType() == typeof(Player)
			);
			return _seePlayer.Value;
		}
	}

	private bool? _reachWall;

	public bool ReachWall {
		get {
			_reachWall ??= _wallChecker.IsColliding();
			return _reachWall.Value;
		}
	}

	private bool? _reachCliff;

	public bool ReachCliff {
		get {
			_reachCliff ??= !_floorChecker1.IsColliding() && !_floorChecker2.IsColliding();
			return _reachCliff.Value;
		}
	}

	private bool? _canMove;

	public bool CanMove {
		get {
			_canMove = !ReachWall && !ReachCliff;
			return _canMove.Value;
		}
	}
}

class DamageEvent {
	public readonly int Damage;
	public bool Processed;

	public DamageEvent(int f) {
		Damage = f;
		Processed = false;
	}
}

public partial class Boar : Enemy, IStateMachineOwner<BoarState> {
	[Export] private int _walkSpeed = 60;
	[Export] private ulong _cooldown = 2500;

	private StateMachine<BoarState> _stateMachine;
	private BoarTmp _tmp;

	private ulong _losePlayerAt;
	private DamageEvent _damageEvent;

	public override void _Ready() {
		base._Ready();

		_stateMachine = new StateMachine<BoarState>(this, true);
		_tmp = new BoarTmp(
			_graphics.GetNode<RayCast2D>("WallChecker"),
			_graphics.GetNode<RayCast2D>("FloorChecker1"),
			_graphics.GetNode<RayCast2D>("FloorChecker2"),
			_graphics.GetNode<RayCast2D>("PlayerChecker")
		);

		OnStateChange(BoarState.Idle, BoarState.Idle);
	}

	public override void _PhysicsProcess(double delta) {
		_tmp.Clear();
		_stateMachine._PhysicsProcess(delta);
	}

	protected override void OnHurt(HitBox from) {
		if (_damageEvent != null) return;

		if (from.Owner.GetType() == typeof(Player)) {
			var player = (Player)from.Owner;
			if (player.CurrentDamage < 1) return;
			_damageEvent = new DamageEvent(player.CurrentDamage);
		}
	}

	public BoarState GetNextState(BoarState current) {
		if (_damageEvent is { Processed: false }) {
			_damageEvent.Processed = true;
			return BoarState.OnHit;
		}

		if (current != BoarState.OnHit && _tmp.SeePlayer) {
			_losePlayerAt = 0;
			return BoarState.Run;
		}

		switch (current) {
			case BoarState.Idle: {
				_losePlayerAt = 0;

				if (_stateMachine.Duration > 2000) {
					return BoarState.Walk;
				}

				break;
			}
			case BoarState.Walk: {
				_losePlayerAt = 0;

				if (!_tmp.CanMove) {
					TurnFace();
					return BoarState.Walk;
				}

				break;
			}
			case BoarState.Run: {
				if (!_tmp.CanMove) {
					TurnFace();
				}

				if (!_tmp.SeePlayer) {
					if (_losePlayerAt == 0) {
						_losePlayerAt = _stateMachine.Duration;
						return BoarState.Run;
					}

					if (_stateMachine.Duration - _losePlayerAt >= _cooldown) {
						return BoarState.Walk;
					}
				}

				break;
			}
			case BoarState.OnHit: {
				if (_stateMachine.FrameCount > 0 && !_animationPlayer.IsPlaying()) {
					_damageEvent = null;
					_losePlayerAt = 0;
					return BoarState.Run;
				}

				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(current), current, null);
			}
		}

		return current;
	}

	public void OnStateChange(BoarState from, BoarState to) {
		switch (to) {
			case BoarState.Idle: {
				_animationPlayer.Play("idle");
				break;
			}
			case BoarState.Walk: {
				_animationPlayer.Play("walk");
				break;
			}
			case BoarState.Run: {
				_animationPlayer.Play("run");
				break;
			}
			case BoarState.OnHit: {
				_animationPlayer.Play("on_hit");
				_hp -= _damageEvent.Damage;
				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(to), to, null);
			}
		}
	}

	public void TickPhysics(BoarState current, double delta) {
		var tmpv = Velocity;

		switch (current) {
			case BoarState.Idle: {
				move(ref tmpv, delta, 0);
				break;
			}
			case BoarState.Walk: {
				move(ref tmpv, delta, _walkSpeed);
				break;
			}
			case BoarState.Run: {
				move(ref tmpv, delta, _speed);
				break;
			}
			case BoarState.OnHit: {
				onHit(ref tmpv, delta, _walkSpeed);
				break;
			}
			default: {
				throw new ArgumentOutOfRangeException(nameof(current), current, null);
			}
		}

		Velocity = tmpv;
		MoveAndSlide();
	}

	private void move(ref Vector2 tmpv, double delta, int speed) {
		tmpv.X = speed * (int)_faceDirection;
		tmpv.Y += (float)(Globals.Gravity * delta);
	}

	private void onHit(ref Vector2 tmpv, double delta, int speed) {
		tmpv.X = speed * (int)_faceDirection * -1;
		tmpv.Y += (float)(Globals.Gravity * delta);
	}
}