using UnityEngine;

public class CharacterContent : MonoBehaviour
{
    CharacterContentFactory originFactory;
    public CharacterContentFactory OriginFactory
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