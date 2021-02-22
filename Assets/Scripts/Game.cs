using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField, Range(2, 128)]
    int xsize = 128;
    [SerializeField, Range(2, 128)]
    int zsize = 128;
    [SerializeField, Range(0.1f, 1.0f)]
    float tileSize = 0.2f;
    [SerializeField]
    Transform ground = default;
    [SerializeField]
    Texture2D tileTexture = default;
    [SerializeField]
    GameTileContentFactory tileContentFactory = default;
    [SerializeField]
    CharacterFactory characterFactory = default;

    List<Character> characters = new List<Character>();
    GridMoveManager moveManager = new GridMoveManager();

    void Awake()
    {
        board.Init(gridX, gridZ, gridSize);
        moveManager.Init(transform.position, gridX, gridZ, gridSize, 1000);
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
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ToggleTile(GameTileType.RedDestination);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ToggleTile(GameTileType.BlueDestination);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ToggleTile(GameTileType.Wall);
        }
        foreach (var c in characters)
        {
            c.Update();
        }
    }
    void FixedUpdate()
    {
        moveManager.Update();
    }
    void AddCharacter(CharacterType type)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            var character = characterFactory.Get(type);
            if (!character.Init(hit.point, Vector3.forward, moveManager))
            {
                return;
            }
            character.transform.position = hit.point;
            characters.Add(character);
        }
    }
    void ToggleTile(GameTileType type)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue) && board.GetTileGrid(hit.point, out int x, out int z))
        {
            board.RemoveTile(x, z);
            if (type == GameTileType.RedDestination)
            {
            }

            board.ToggleTile(type, hit.point);
            if (type == GameTileType.RedDestination)
            {
                redDestinationPos = hit.point;
                redDestinationChange = true;
            }
        }
    }
}