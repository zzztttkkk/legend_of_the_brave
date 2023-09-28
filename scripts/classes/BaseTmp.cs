using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LegendOfTheBrave.scripts.classes;

public class BaseTmp {
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