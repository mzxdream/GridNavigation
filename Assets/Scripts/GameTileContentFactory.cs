using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/GameTileContentFactory")]
public class GameTileContentFactory : GameObjectFactory
{
    [SerializeField]
    GameTileContent wall = default;
    [SerializeField]
    GameTileContent redDestination = default;
    [SerializeField]
    GameTileContent blueDestination = default;

    public GameTileContent GetPrefab(GameTileType type)
    {
        switch (type)
        {
            case GameTileType.Wall: return wall;
            case GameTileType.RedDestination: return redDestination;
            case GameTileType.BlueDestination: return blueDestination;
        }
        Debug.Assert(false, "unsupported type:" + type);
        return null;
    }
    public GameTileContent Get(GameTileType type)
    {
        var prefab = GetPrefab(type);
        var instance = CreateGameObjectInstance(prefab);
        instance.OriginFactory = this;
        return instance;
    }
    public void Reclaim(GameTileContent tileContent)
    {
        Debug.Assert(tileContent.OriginFactory == this, "wrong factory reclaimed");
        Destroy(tileContent.gameObject);
    }
}