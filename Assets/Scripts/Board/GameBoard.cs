using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [SerializeField]
    Transform ground = default;
    [SerializeField]
    Texture2D gridTexture = default;
    [SerializeField]
    GameTileFactory tileFactory = default;
    int gridX;
    int gridZ;
    float gridSize;
    Dictionary<int, GameTile> tiles = new Dictionary<int, GameTile>();

    public bool Init(int gridX, int gridZ, float gridSize)
    {
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.gridSize = gridSize;

        ground.localScale = new Vector3(gridX * gridSize, gridZ * gridSize, 1f);
        var material = ground.GetComponent<MeshRenderer>().material;
        material.mainTexture = gridTexture;
        material.SetTextureScale("_MainTex", new Vector2(gridX, gridZ));
        return true;
    }
    public void Clear()
    {
        foreach (var tile in tiles)
        {
        }
    }
    public bool GetTileGrid(Vector3 pos, out int tx, out int tz)
    {
        tx = (int)((pos.x + gridX * gridSize / 2) / gridSize);
        tz = (int)((pos.z + gridZ * gridSize / 2) / gridSize);
        return tx >= 0 && tx < gridX && tz >= 0 && tz < gridZ;
    }
    public Vector3 GetTilePos(int tx, int tz)
    {
        Debug.Assert(tx >= 0 && tx < gridX && tz >= 0 && tz < gridZ);
        float x = (tx + 0.5f - gridX * 0.5f) * gridSize;
        float z = (tz + 0.5f - gridZ * 0.5f) * gridSize;
        return new Vector3(x, 0, z);
    }
    public bool ToggleTile(GameTileType type, Vector3 pos)
    {
        if (!GetTileGrid(pos, out var tx, out var tz))
        {
            return false;
        }
        int key = tx + tz * gridX;
        bool isOnlyRemove = false;
        if (tiles.TryGetValue(key, out var tile))
        {
            isOnlyRemove = tile.Type == type;
            tile.Clear();
            tiles.Remove(key);
        }
        if (!isOnlyRemove)
        {
            tile = tileFactory.Get(type);
            tile.transform.localPosition = GetTilePos(tx, tz);
            tile.transform.localScale = new Vector3(gridSize, gridSize, gridSize);
            tiles.Add(key, tile);
        }
        return false;
    }
}