using UnityEngine;

public enum GameTileType { Wall, RedDestination, BlueDestination }

public class GameTile : MonoBehaviour
{
    [SerializeField]
    GameTileType type = default;
    public GameTileType Type { get => type; }
    public int key;
    GameTileFactory originFactory;
    public GameTileFactory OriginFactory
    {
        get => originFactory;
        set
        {
            Debug.Assert(originFactory == null, "Redefined origin factory");
            originFactory = value;
        }
    }

    public void Init()
    {
    }
    public void Clear()
    {
        originFactory.Reclaim(this);
    }
}