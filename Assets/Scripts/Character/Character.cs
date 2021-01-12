using UnityEngine;

public enum CharacterType { RedSmall, RedMedium, RedLarge, BlueSmall, BlueMedium, BlueLarge }

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

    public void Init(float Speed, float radius)
    {
        model.localScale = new Vector3(radius, radius, radius);
    }
    public void Clear()
    {
        originFactory.Reclaim(this);
    }
}