﻿using System.Collections.Generic;
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
    CharacterContentFactory characterContentFactory = default;

    Dictionary<int, GameTile> tiles;
    List<Character> characters;
    GridMoveManager moveManager;
    int redDestinationIndex;
    int blueDestinationIndex;

    void Awake()
    {
        tiles = new Dictionary<int, GameTile>();
        ground.localScale = new Vector3(xsize * tileSize, zsize * tileSize, 1f);
        var material = ground.GetComponent<MeshRenderer>().material;
        material.mainTexture = tileTexture;
        material.SetTextureScale("_MainTex", new Vector2(xsize, zsize));

        characters = new List<Character>();

        moveManager = new GridMoveManager();
        moveManager.Init(transform.position, xsize, zsize, tileSize, 1000);

        redDestinationIndex = -1;
        blueDestinationIndex = -1;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddCharacter(CharacterType.Red);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddCharacter(CharacterType.Blue);
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
            var character = new Character(hit.point, Vector3.forward, type, characterContentFactory, moveManager);
            characters.Add(character);
        }
    }
    void ToggleTile(GameTileType type)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue) )
        {
&& board.GetTileGrid(hit.point, out int x, out int z)
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