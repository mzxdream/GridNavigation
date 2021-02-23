using UnityEngine;

public enum GameTileType { Wall, RedDestination, BlueDestination }

public class GameTile
{
    private readonly GameTileContent content;
    private readonly GameTileType type;
    private readonly int index;

    public GameTileType Type { get => type; }
    public int Index { get => index; }

    public GameTile(GameTileContent content, GameTileType type, int index)
    {
        this.content = content;
        this.type = type;
        this.index = index;
    }
    public void SetPosition(Vector3 position)
    {
        content.SetPosition(position);
    }
    public void SetForward(Vector3 forward)
    {
        content.SetForward(forward);
    }
    public void SetScale(Vector3 scale)
    {
        content.SetScale(scale);
    }
    public void Clear()
    {
        content.Clear();
    }
}