using UnityEngine;

public class GameTileContent : MonoBehaviour
{
    GameTileContentFactory originFactory;
    public GameTileContentFactory OriginFactory
    {
        get => originFactory;
        set
        {
            Debug.Assert(originFactory == null, "redefined origin factory");
            originFactory = value;
        }
    }
    public void Recycle()
    {
        originFactory.Reclaim(this);
    }
}