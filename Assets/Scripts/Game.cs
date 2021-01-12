using System.Collections.Generic;
using UnityEngine;

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
            AddCharacter(CharacterType.RedMedium);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddCharacter(CharacterType.BlueMedium);
        }
        if (Input.GetMouseButtonDown(0))
        {
            ToggleTile(GameTileType.Wall);
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                ToggleTile(GameTileType.BlueDestination);
            }
            else
            {
                ToggleTile(GameTileType.RedDestination);
            }
        }
    }
    void AddCharacter(CharacterType type)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            var character = characterFactory.Get(type);
            character.transform.position = hit.point;
            characters.Add(character);
        }
    }
    void ToggleTile(GameTileType type)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            board.ToggleTile(type, hit.point);
        }
    }
}