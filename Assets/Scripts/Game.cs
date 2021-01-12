using System.Collections.Generic;
using UnityEngine;

public enum OperatorType { AddRed, AddBlue, SetWall, SetDestination }
public class Game : MonoBehaviour
{
    [SerializeField, Range(2, 128)]
    int gridX = 33;
    [SerializeField, Range(2, 128)]
    int gridZ = 33;
    [SerializeField, Range(0.1f, 1.0f)]
    float gridSize = 0.5f;
    [SerializeField]
    GameBoard board = default;
    [SerializeField]
    CharacterFactory characterFactory = default;
    static Game instance;
    public static Game Instance => instance;
    OperatorType operatorType;
    List<Character> characters = new List<Character>();

    void Awake()
    {
        board.Init(gridX, gridZ, gridSize);
    }
    void OnEnable()
    {
        instance = this;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            operatorType = OperatorType.AddRed;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            operatorType = OperatorType.AddBlue;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            operatorType = OperatorType.SetWall;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            operatorType = OperatorType.SetDestination;
        }
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                OnMouseLeftDown(hit.point);
            }
        }
    }
    void AddCharacter(CharacterType type, Vector3 pos)
    {
        var character = characterFactory.Get(type);
        character.transform.position = pos;
        characters.Add(character);
    }
    void OnMouseLeftDown(Vector3 pos)
    {
        switch (operatorType)
        {
            case OperatorType.AddRed: AddCharacter(CharacterType.RedMedium, pos); break;
            case OperatorType.AddBlue: AddCharacter(CharacterType.BlueMedium, pos); break;
            case OperatorType.SetWall: board.ToggleTile(GameTileType.Wall, pos); break;
            case OperatorType.SetDestination: board.ToggleTile(GameTileType.RedDestination, pos); break;
        }
    }
}