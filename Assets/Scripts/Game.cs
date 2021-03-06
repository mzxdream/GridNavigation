﻿using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    Transform ground = default;
    [SerializeField]
    Texture2D tileTexture = default;
    [SerializeField]
    GameTileAsset wallPrefab = default, redDestinationPrefab = default, blueDestinationPrefab = default;
    [SerializeField]
    CharacterAsset redCharacterPrefab = default, blueCharacterPrefab = default;
    [SerializeField, Range(2, 128)]
    int xsize = 128;
    [SerializeField, Range(2, 128)]
    int zsize = 128;
    [SerializeField, Range(0.1f, 1.0f)]
    float tileSize = 0.2f;

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

        var offset = new Vector3(xsize * tileSize * 0.5f, 0, zsize * tileSize * 0.5f);
        moveManager = new GridMoveManager();
        moveManager.Init(transform.position - offset, transform.position + offset, tileSize, 30, 1000);

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
            SetTile(GameTileType.RedDestination);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetTile(GameTileType.BlueDestination);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetTile(GameTileType.Wall);
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
            CharacterAsset prefab = null;
            switch (type)
            {
                case CharacterType.Red:
                    prefab = redCharacterPrefab;
                    break;
                case CharacterType.Blue:
                    prefab = blueCharacterPrefab;
                    break;
                default:
                    Debug.Assert(false, "unsupported character type:" + type);
                    break;
            }
            var character = new Character(prefab, type, hit.point, Vector3.forward, 0.6f, moveManager);
            characters.Add(character);
        }
    }
    bool RemoveTile(int index)
    {
        if (tiles.TryGetValue(index, out var tile))
        {
            if (tile.Type == GameTileType.Wall)
            {
                moveManager.SetTileBlocked(index % xsize, index / xsize, false);
            }
            else if (tile.Type == GameTileType.RedDestination)
            {
                redDestinationIndex = -1;
            }
            else if (tile.Type == GameTileType.BlueDestination)
            {
                blueDestinationIndex = -1;
            }
            tile.Clear();
            tiles.Remove(index);
            return true;
        }
        return false;
    }
    Vector3 GetTilePos(int index)
    {
        var x = index % xsize;
        var z = index / xsize;
        var px = (x - xsize * 0.5f + 0.5f) * tileSize;
        var pz = (z - zsize * 0.5f + 0.5f) * tileSize;
        return new Vector3(px, 0.0f, pz);
    }
    bool AddTile(int index, GameTileType type)
    {
        if (tiles.ContainsKey(index))
        {
            return false;
        }
        GameTileAsset prefab = null;
        if (type == GameTileType.Wall)
        {
            prefab = wallPrefab;
            moveManager.SetTileBlocked(index % xsize, index / xsize, true);
        }
        else if (type == GameTileType.RedDestination)
        {
            RemoveTile(redDestinationIndex);
            prefab = redDestinationPrefab;
            redDestinationIndex = index;
        }
        else if (type == GameTileType.BlueDestination)
        {
            RemoveTile(blueDestinationIndex);
            prefab = blueDestinationPrefab;
            blueDestinationIndex = index;
        }
        else
        {
            return false;
        }
        var tile = new GameTile(prefab, type, index, GetTilePos(index), tileSize);
        tiles.Add(index, tile);
        return true;
    }
    void SetTile(GameTileType type)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        var pos = hit.point;
        int x = (int)((pos.x + xsize * tileSize * 0.5f) / tileSize);
        int z = (int)((pos.z + zsize * tileSize * 0.5f) / tileSize);
        if (x < 0 || x >= xsize || z < 0 || z >= zsize)
        {
            return;
        }
        var index = x + z * xsize;
        if (RemoveTile(index))
        {
            return;
        }
        AddTile(index, type);
        if (type == GameTileType.RedDestination)
        {
            foreach (var c in characters)
            {
                if (c.Type == CharacterType.Red)
                {
                    c.StartMoving(pos);
                }
            }
        }
        else if (type == GameTileType.BlueDestination)
        {
            foreach (var c in characters)
            {
                if (c.Type == CharacterType.Blue)
                {
                    c.StartMoving(pos);
                }
            }
        }
    }
}