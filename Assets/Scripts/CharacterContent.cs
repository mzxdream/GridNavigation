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
}