using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    Transform model = default;
    CharacterFactory factory;
    float speed;

    public CharacterFactory Factory
    {
        get => factory;
        set
        {
            Debug.Assert(factory == null, "redifined factory!");
            factory = value;
        }
    }

    public void Init(float speed)
    {
        this.speed = speed;
    }
    public void Clear()
    {
        //factory.Reclaim(this);
    }
}