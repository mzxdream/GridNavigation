using UnityEngine;

public enum CharacterType { Red, Blue }

public class Character : MonoBehaviour
{
    [SerializeField]
    Transform model = default;
    CharacterFactory originFactory;
    public CharacterFactory OriginFactory
    {
        get => originFactory;
        set
        {
            Debug.Assert(originFactory == null, "redifined factory!");
            originFactory = value;
        }
    }
    float speed;

    public void Init()
    {
    }
    public void Clear()
    {
        originFactory.Reclaim(this);
    }
}