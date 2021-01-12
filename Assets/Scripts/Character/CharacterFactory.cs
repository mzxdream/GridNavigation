using UnityEngine;

[System.Serializable]
class CharacterConfig
{
    public Character prefab = default;
    public float radius = 0.5f;
    public float speed = 0.1f;
}

[CreateAssetMenu(menuName = "ScriptableObject/CharacterFactory")]
public class CharacterFactory : GameObjectFactory
{
    [SerializeField]
    CharacterConfig redSmall = default, redMedium = default, redLarge = default;
    [SerializeField]
    CharacterConfig blueSmall = default, blueMedium = default, blueLarge = default;

    CharacterConfig GetConfig(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.RedSmall: return redSmall;
            case CharacterType.RedMedium: return redMedium;
            case CharacterType.RedLarge: return redLarge;
            case CharacterType.BlueSmall: return blueSmall;
            case CharacterType.BlueMedium: return blueMedium;
            case CharacterType.BlueLarge: return blueLarge;
        }
        Debug.Assert(false, "Unsupported character type");
        return null;
    }
    public Character Get(CharacterType type)
    {
        var config = GetConfig(type);
        var instance = CreateGameObjectInstance(config.prefab);
        instance.OriginFactory = this;
        instance.Init(config.speed, config.radius);
        return instance;
    }
    public void Reclaim(Character character)
    {
        Debug.Assert(character.OriginFactory == this, "Wrong factory reclaimed");
        Destroy(character.gameObject);
    }
}