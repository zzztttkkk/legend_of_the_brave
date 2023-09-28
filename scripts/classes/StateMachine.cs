using System;

namespace LegendOfTheBrave.scripts.classes;

public interface IStateMachineOwner<T> where T : Enum {
	T GetNextState(T current);
	void OnStateChange(T from, T to);
	void TickPhysics(T current, double delta);
}

public class StateMachine<T> where T : Enum {
	private T _current;
	private readonly IStateMachineOwner<T> _owner;

	private T current {
		set {
			_owner.OnStateChange(_current, value);
			_current = value;
		}
	}

	public StateMachine(IStateMachineOwner<T> obj) {
		_owner = obj;
	}


	public void _PhysicsProcess(double delta) {
		while (true) {
			var next = _owner.GetNextState(_current);
			if (Equals(next, _current)) {
				break;
			}

			current = next;
		}

		_owner.TickPhysics(_current, delta);
	}
}