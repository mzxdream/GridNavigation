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

    int xsize;
    int zsize;
    float gridSize;
    Dictionary<int, GameTile> tiles;

    public bool Init(int gridX, int gridZ, float gridSize)
    {
        this.xsize = gridX;
        this.zsize = gridZ;
        this.gridSize = gridSize;
        this.tiles = new Dictionary<int, GameTile>();

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
            tile.Value.Clear();
        }
        tiles.Clear();
    }
    public bool GetTileGrid(Vector3 pos, out int x, out int z)
    {
        x = (int)((pos.x + xsize * gridSize / 2) / gridSize);
        z = (int)((pos.z + zsize * gridSize / 2) / gridSize);
        return x >= 0 && x < xsize && z >= 0 && z < zsize;
    }
    public Vector3 GetTilePos(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        float tx = (x - xsize * 0.5f + 0.5f) * gridSize;
        float tz = (z - zsize * 0.5f + 0.5f) * gridSize;
        return new Vector3(tx, 0, tz);
    }
    public void AddTile(int x, int z, GameTileType type)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        var index = x + z * xsize;
        Debug.Assert(tiles.ContainsKey(index));

        var tile = tileFactory.Get(type);
        tile.transform.localPosition = GetTilePos(x, z);
        tile.transform.localScale = new Vector3(gridSize, gridSize, gridSize);
        tile.Index = index;
        tiles.Add(index, tile);
    }
    public bool RemoveTile(int x, int z)
    {
        Debug.Assert(x >= 0 && x < xsize && z >= 0 && z < zsize);
        var index = x + z * xsize;
        if (tiles.TryGetValue(index, out var tile))
        {
            tile.Clear();
            tiles.Remove(index);
            return true;
        }
        return false;
    }
}