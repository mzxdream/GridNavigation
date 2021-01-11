using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/GameTileContentFactory")]
public class GameTileContentFactory : GameObjectFactory
{
    [SerializeField]
    GameTileContent emptyPrefab = default;
    [SerializeField]
    GameTileContent spawnPoint = default;
    [SerializeField]
    GameTileContent destinationPrefab = default;
    [SerializeField]
    GameTileContent wallPrefab = default;

    T Get<T>(T prefab) where T : GameTileContent
    {
        T instance = CreateGameObjectInstance(prefab);
        instance.OriginFactory = this;
        return instance;
    }
    public GameTileContent Get(GameTileContentType type)
    {
        switch (type)
        {
            case GameTileContentType.Empty: return Get(emptyPrefab);
            case GameTileContentType.SpawnPoint: return Get(spawnPoint);
            case GameTileContentType.Destination: return Get(destinationPrefab);
            case GameTileContentType.Wall: return Get(wallPrefab); 
        }
        Debug.Assert(false, "Unsupported type:" + type);
        return null;
    }
    public void Reclaim(GameTileContent content)
    {
        Debug.Assert(content.OriginFactory == this, "Wrong factory reclaimed");
        Destroy(content.gameObject);
    }
}