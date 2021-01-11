using System.Collections.Generic;
using UnityEngine;

public class CharacterCollection : MonoBehaviour
{
    [SerializeField]
    CharacterFactory characterFactory = default;
    List<Character> characters = new List<Character>();

    public bool Init()
    {
        return true;
    }
    public void Clear()
    {
    }
    public Character AddCharacter(float x, float z, CharacterType type)
    {
        var character = characterFactory.Get(type);
        character.transform.SetParent(transform, false);
        character.transform.localPosition = new Vector3(x, 0, z);
        characters.Add(character);
        return character;
    }
}