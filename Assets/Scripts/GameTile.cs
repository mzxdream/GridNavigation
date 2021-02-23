using UnityEngine;

public enum GameTileType { Wall, RedDestination, BlueDestination }

public class GameTile
{
    private readonly GameTileAsset asset;
    private readonly GameTileType type;
    private readonly int index;

    public GameTileType Type { get => type; }
    public int Index { get => index; }

    public GameTile(GameTileAsset prefab, GameTileType type, int index)
    {
        this.asset = GameObject.Instantiate(prefab);
        this.type = type;
        this.index = index;
    }
    public void SetPosition(Vector3 position)
    {
        asset.SetPosition(position);
    }
    public void SetForward(Vector3 forward)
    {
        asset.SetForward(forward);
    }
    public void SetScale(Vector3 scale)
    {
        asset.SetScale(scale);
    }
    public void Clear()
    {
        asset.Clear();
    }
}