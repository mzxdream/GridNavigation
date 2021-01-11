using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/CharacterFactory")]
public class CharacterFactory : GameObjectFactory
{
    [SerializeField]
    Character redPrefab = default;
    [SerializeField]
    Character bluePrefab = default;

    T Get<T>(T prefab) where T : Character
    {
        T instance = CreateGameObjectInstance(prefab);
        instance.OriginFactory = this;
        instance.Init();
        return instance;
    }
    public Character Get(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Red: return Get(redPrefab);
            case CharacterType.Blue: return Get(bluePrefab);
        }
        Debug.Assert(false, "Unsupported type:" + type);
        return null;
    }
    public void Reclaim(Character character)
    {
        Debug.Assert(character.OriginFactory == this, "Wrong factory reclaimed");
        Destroy(character.gameObject);
    }
}