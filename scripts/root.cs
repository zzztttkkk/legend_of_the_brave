using Godot;
using LegendOfTheBrave.scripts;

public partial class root : Node2D {
	private TileMap _tileMap;
	private Camera2D _camera2D;
	private CanvasLayer _ui;

	public override void _Ready() {
		_tileMap = GetNode<TileMap>("TileMap");
		_camera2D = GetNode<Camera2D>("Player/Camera2D");
		_ui = GetNode<CanvasLayer>("uis/ui");

		var rect = _tileMap.GetUsedRect().Grow(-1);
		var size = _tileMap.TileSet.TileSize;

		_camera2D.LimitTop = rect.Position.Y * size.Y;
		_camera2D.LimitBottom = rect.End.Y * size.Y;
		_camera2D.LimitLeft = rect.Position.X * size.X;
		_camera2D.LimitRight = rect.End.X * size.Y;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionReleased("paused")) {
			if (@event.GetMeta(Names.PausedEventHandledMetaKey, @default: false).AsBool()) {
				return;
			}

			GetTree().Paused = true;
			_ui.Show();
		}
	}
}