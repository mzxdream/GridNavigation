using UnityEngine;

[System.Serializable]
class CharacterConfig
{
    public Character prefab = default;
    public float speed = 1.0f;
}

[CreateAssetMenu]
public class CharacterFactory : GameObjectFactory
{

    [SerializeField]
    CharacterConfig[] configs;
    public Character GetCharacter(int index)
    {
        //Debug.Assert(index >= 0 && index < configs.Length);
        if (index < 0 || index >= configs.Length)
        {
            Debug.LogError("index is error " + index);
            return null;
        }
        var config = configs[index];
        Character instance = CreateGameObjectInstance(config.prefab);
        instance.Factory = this;
        instance.Init(config.speed);
        return instance;
    }
}