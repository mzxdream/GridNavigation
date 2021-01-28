using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/CharacterFactory")]
public class CharacterFactory : GameObjectFactory
{
    [SerializeField]
    Character redSmall = default, redMedium = default, redLarge = default;
    [SerializeField]
    Character blueSmall = default, blueMedium = default, blueLarge = default;

    Character GetPrefab(CharacterType type)
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
        var prefab = GetPrefab(type);
        var instance = CreateGameObjectInstance(prefab);
        instance.OriginFactory = this;
        return instance;
    }
    public void Reclaim(Character character)
    {
        Debug.Assert(character.OriginFactory == this, "Wrong factory reclaimed");
        Destroy(character.gameObject);
    }
}