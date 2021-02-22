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
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    public void SetForward(Vector3 forward)
    {
        transform.forward = forward;
    }
    public void SetScale(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }
}