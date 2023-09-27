using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public interface IStateMachineOwner<T> where T : Enum {
	T GetNextState(T current);
	void OnStateChange(T from, T to);
	void TickPhysics(T current, double delta);
}


public class BaseTicksSharedData {
	private static readonly Dictionary<object, List<FieldInfo>> InfosMap = new();

	public void Clear() {
		var type = GetType();
		var ok = InfosMap.TryGetValue(type, out var lst);
		if (!ok) {
			var tmp = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(field => field.FieldType.IsGenericType)
				.Where(field => field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)).ToList();
			InfosMap[type] = tmp;
			lst = tmp;
		}

		foreach (var field in lst) {
			field.SetValue(this, null);
		}
	}
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