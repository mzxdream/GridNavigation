using System.Collections.Generic;
using UnityEngine;

public enum OperatorType { AddRed, AddBlue, SetWall, SetDestination }
public class Game : MonoBehaviour
{
    [SerializeField]
    Vector2Int boardSize = new Vector2Int(16, 16);
    [SerializeField, Range(1, 10)]
    int scale = 4;
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
        board.Init(boardSize, scale);
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
            case OperatorType.SetWall: board.ToggleTileContent(GameTileContentType.Wall, pos); break;
            case OperatorType.SetDestination: board.ToggleTileContent(GameTileContentType.Destination, pos); break;
        }
    }
}