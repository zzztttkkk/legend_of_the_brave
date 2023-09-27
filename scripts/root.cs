using Godot;

public partial class root : Node2D {
	private TileMap _tileMap;
	private Camera2D _camera2D;

	public override void _Ready() {
		GetWindow().Size = new Vector2I(960, 540);
		GetWindow().Position = new Vector2I(60, 60);

		_tileMap = GetNode<TileMap>("TileMap");
		_camera2D = GetNode<Camera2D>("Player/Camera2D");

		var rect = _tileMap.GetUsedRect().Grow(-1);
		var size = _tileMap.TileSet.TileSize;

		_camera2D.LimitTop = rect.Position.Y * size.Y;
		_camera2D.LimitBottom = rect.End.Y * size.Y;
		_camera2D.LimitLeft = rect.Position.X * size.X;
		_camera2D.LimitRight = rect.End.X * size.Y;
	}
}