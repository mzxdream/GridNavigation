using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/GameTileFactory")]
public class GameTileFactory : GameObjectFactory
{
    [SerializeField]
    GameTile wall = default, redDestination = default, blueDestination = default;

    public GameTile GetPrefab(GameTileType type)
    {
        switch (type)
        {
            case GameTileType.Wall: return wall;
            case GameTileType.RedDestination: return redDestination;
            case GameTileType.BlueDestination: return blueDestination;
        }
        Debug.Assert(false, "Unsupported type:" + type);
        return null;
    }
    public GameTile Get(GameTileType type)
    {
        var prefab = GetPrefab(type);
        var instance = CreateGameObjectInstance(prefab);
        instance.OriginFactory = this;
        instance.Init();
        return instance;
    }

    public void Reclaim(GameTile tile)
    {
        Debug.Assert(tile.OriginFactory == this, "Wrong factory reclaimed");
        Destroy(tile.gameObject);
    }
}