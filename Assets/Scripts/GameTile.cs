using UnityEngine;

public enum GameTileType { Wall, RedDestination, BlueDestination }

public class GameTile
{
    private readonly int index;
    private readonly GameTileType type;
    private readonly GameTileContent content;

    public int Index { get => index; }
    public GameTileType Type { get => type; }

    public GameTile(int index, GameTileType type, GameTileContentFactory contentFactory)
    {
        this.index = index;
        this.type = type;
        this.content = contentFactory.Get(type);
    }
    public void SetPosition(Vector3 position)
    {
        content.SetPosition(position);
    }
    public void SetForward(Vector3 forward)
    {
        content.SetForward(forward);
    }
    public void Recycle()
    {
        content.Recycle();
    }
}