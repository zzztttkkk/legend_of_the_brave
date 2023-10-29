using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using LegendOfTheBrave.scripts;
using LegendOfTheBrave.scripts.classes;

public enum BoarState {
	Idle,
	Walk,
	Run,
	OnHit,
	Die,
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
	public readonly Node2D Source;

	public DamageEvent(Node2D source, int f) {
		Damage = f;
		Processed = false;
		Source = source;
	}
}

public partial class Boar : Enemy, IStateMachineOwner<BoarState> {
	[Export] private int _walkSpeed = 60;
	[Export] private ulong _cooldown = 2500;

	private StateMachine<BoarState> _stateMachine;
	private BoarTmp _tmp;

	private ulong _losePlayerAt;
	private LinkedList<DamageEvent> _damages;

	public override void _Ready() {
		base._Ready();

		_hp = 200;
		_damages = new LinkedList<DamageEvent>();
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
		if (from.Owner.GetType() == typeof(Player)) {
			var player = (Player)from.Owner;
			if (player.CurrentDamage < 1) return;
			_damages.AddLast(new DamageEvent(player, player.CurrentDamage));
		}
	}

	public BoarState GetNextState(BoarState current) {
		if (_hp <= 0) {
			if (current == BoarState.Die && _stateMachine.FrameCount > 0 && !_animationPlayer.IsPlaying()) {
				GD.Print("XXXXXXXXXXXXX");
				QueueFree();
				return BoarState.Die;
			}

			return BoarState.Die;
		}

		if (_damages.First != null) {
			if (_damages.Any(evt => {
				    var np = !evt.Processed;
				    evt.Processed = true;
				    return np;
			    })) {
				return BoarState.OnHit;
			}
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
					_damages.Clear();
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

				foreach (var evt in _damages) {
					_hp -= evt.Damage;
				}

				break;
			}
			case BoarState.Die: {
				_animationPlayer.Play("die");
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
				onHit(ref tmpv, delta, _speed * (float)1.2);
				break;
			}
			case BoarState.Die: {
				tmpv.X = 0;
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

	private void onHit(ref Vector2 tmpv, double delta, float speed) {
		if (_damages.First == null) return;

		var xDir = _damages.First.ValueRef.Source.GlobalPosition.DirectionTo(this.GlobalPosition).X;
		if ((xDir > 0 && _faceDirection != FaceDirection.Left) ||
		    (xDir < 0 && _faceDirection != FaceDirection.Right)) {
			TurnFace();
		}

		tmpv.X = speed * xDir;
		tmpv.Y += (float)(Globals.Gravity * delta);
	}
}