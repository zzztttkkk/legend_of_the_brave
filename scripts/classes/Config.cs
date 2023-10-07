using System.IO;
using Godot;
using IniParser;
using IniParser.Model;

namespace LegendOfTheBrave.scripts.classes;

public class Config {
	private IniData _CfgIniData;

	public Config() {
		load();
	}

	private void load() {
		var cwd = Directory.GetCurrentDirectory();
		if (File.Exists($"{cwd}/config.ini")) {
			_CfgIniData = new FileIniDataParser().ReadFile($"{cwd}/config.ini");
		}

		_CfgIniData ??= new IniData();
		if (File.Exists($"{cwd}/config.local.ini")) {
			_CfgIniData.Merge(new FileIniDataParser().ReadFile($"{cwd}/config.local.ini"));
		}
	}

	public string GetString(string path) {
		return _CfgIniData.TryGetKey(path, out var val) ? val : "";
	}

	public int? GetInt(string path) {
		var tmp = GetString(path);
		if (string.IsNullOrEmpty(tmp)) {
			return null;
		}

		if (int.TryParse(tmp, out var val)) {
			return val;
		}

		return null;
	}

	public Vector2I? GetVector2I(string path, string sep = "*") {
		var tmp = GetString(path);
		if (string.IsNullOrEmpty(tmp)) {
			return null;
		}

		var parts = tmp.Split(sep);
		if (parts.Length < 2) {
			return null;
		}

		if (!int.TryParse(parts[0], out var x)) {
			return null;
		}

		if (!int.TryParse(parts[1], out var y)) {
			return null;
		}

		return new Vector2I(x, y);
	}

	public Vector2? GetVector2(string path, string sep = "*") {
		var tmp = GetString(path);
		if (string.IsNullOrEmpty(tmp)) {
			return null;
		}

		var parts = tmp.Split(sep);
		if (parts.Length < 2) {
			return null;
		}

		if (!float.TryParse(parts[0], out var x)) {
			return null;
		}

		if (!float.TryParse(parts[1], out var y)) {
			return null;
		}

		return new Vector2(x, y);
	}

	public bool? GetBool(string path) {
		var tmp = GetString(path);
		if (string.IsNullOrEmpty(tmp)) {
			return null;
		}

		if (!bool.TryParse(tmp, out var val)) {
			return val;
		}

		return null;
	}
}