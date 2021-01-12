using UnityEngine;

public enum CharacterType { RedSmall, RedMedium, RedLarge, BlueSmall, BlueMedium, BlueLarge }

public class Character : MonoBehaviour
{
    [SerializeField]
    Transform model = default;
    [SerializeField]
    float radius = 0.5f;
    public float Radius { get => radius; }
    [SerializeField]
    float maxSpeed = 1.0f;
    public float MaxSpeed { get => maxSpeed; }
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
    public void Init()
    {
        model.localScale = new Vector3(radius, radius, radius);
    }
    public void Clear()
    {
        originFactory.Reclaim(this);
    }
}