using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/CharacterContentFactory")]
public class CharacterContentFactory : GameObjectFactory
{
    [SerializeField]
    CharacterContent redCharacter = default;
    [SerializeField]
    CharacterContent blueCharacter = default;

    CharacterContent GetPrefab(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Red: return redCharacter;
            case CharacterType.Blue: return blueCharacter;
        }
        Debug.Assert(false, "unsupported character type");
        return null;
    }
    public CharacterContent Get(CharacterType type)
    {
        var prefab = GetPrefab(type);
        var instance = CreateGameObjectInstance(prefab);
        instance.OriginFactory = this;
        return instance;
    }
    public void Reclaim(CharacterContent characterContent)
    {
        Debug.Assert(characterContent.OriginFactory == this, "wrong factory reclaimed");
        Destroy(characterContent.gameObject);
    }
}