using UnityEngine;

public enum GameTileType { Wall, RedDestination, BlueDestination }

public class GameTile
{
    private readonly GameTileAsset asset;
    private readonly GameTileType type;
    private readonly int index;

    public GameTileType Type { get => type; }
    public int Index { get => index; }

    public GameTile(GameTileAsset prefab, GameTileType type, int index, Vector3 position, float tileSize)
    {
        asset = GameObject.Instantiate(prefab);
        this.type = type;
        this.index = index;
        asset.transform.position = position;
        asset.transform.localScale = new Vector3(tileSize, tileSize, tileSize);
    }
    public void Clear()
    {
        asset.Clear();
    }
}