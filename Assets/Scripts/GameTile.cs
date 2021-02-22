public enum GameTileType { Wall, RedDestination, BlueDestination }

public class GameTile
{
    private readonly GameTileType type;
    private readonly int index;
    private readonly GameTileContent content;

    public GameTileType Type { get => type; }
    public int Index { get => index; }

    public GameTile(GameTileType type, int index, GameTileContent content)
    {
        this.type = type;
        this.index = index;
        this.content = content;
    }
    public void Recycle()
    {
        content.Recycle();
    }
}