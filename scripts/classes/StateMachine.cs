using System;
using Godot;

namespace LegendOfTheBrave.scripts.classes;

public interface IStateMachineOwner<T> where T : Enum {
	T GetNextState(T current);
	void OnStateChange(T from, T to);
	void TickPhysics(T current, double delta);
}

public class StateMachine<T> where T : Enum {
	private T _current;
	private readonly IStateMachineOwner<T> _owner;
	private readonly bool _debugPrint;

	private ulong _frameCount;
	private ulong _duration;

	public T current {
		get => _current;

		private set {
			if (!Equals(_current, value)) {
				_frameCount = 0;
				_duration = 0;
			}

			_owner.OnStateChange(_current, value);
			_current = value;
		}
	}

	public ulong FrameCount => _frameCount;

	public ulong Duration => _duration;

	public StateMachine(IStateMachineOwner<T> obj, bool debugPrint = false) {
		_owner = obj;
		_debugPrint = debugPrint;
	}

	public void _PhysicsProcess(double delta) {
		while (true) {
			var next = _owner.GetNextState(_current);
			if (Equals(next, _current)) {
				break;
			}

			if (_debugPrint) {
				var name = $"{_owner.GetType().Name}#{_owner.GetHashCode()}";
				if (_owner.GetType().IsSubclassOf(typeof(Node2D))) {
					name = $"{((Node2D)_owner).Name}";
				}

				GD.Print($"[{name}] [{Engine.GetPhysicsFrames()}] {_current} => {next}");
			}

			current = next;
		}

		_owner.TickPhysics(_current, delta);
		_frameCount++;
		_duration += (ulong)(delta * 1000);
	}
}