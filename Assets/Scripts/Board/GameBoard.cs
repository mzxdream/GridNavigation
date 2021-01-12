using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [SerializeField]
    Transform ground = default;
    [SerializeField]
    Texture2D gridTexture = default;
    [SerializeField]
    GameTile tilePrefab = default;
    [SerializeField]
    GameTileContentFactory tileContentFactory = default;
    Vector2Int size;
    int scale;
    Dictionary<int, GameTile> tiles;

    public bool Init(Vector2Int size, int scale)
    {
        tiles = new Dictionary<int, GameTile>();
        this.size = size;
        this.scale = scale;

        ground.localScale = new Vector3(size.x, size.y, 1f);
        var material = ground.GetComponent<MeshRenderer>().material;
        material.mainTexture = gridTexture;
        material.SetTextureScale("_MainTex", size * scale);
        return true;
    }
    public void Clear()
    {
    }
    void OnValidate()
    {
        if (size.x < 2)
        {
            size.x = 2;
        }
        if (size.y < 2)
        {
            size.y = 2;
        }
    }
    public int GetTileKey(float x, float z)
    {
        int tx = (int)((x + size.x * 0.5) * scale);
        int tz = (int)((z + size.y * 0.5) * scale);
        if (tx < 0 || tx >= size.x * scale || tz < 0 || tz >= size.y * scale)
        {
            return -1;
        }
        return tx + tz * size.x * scale;
    }
    public Vector3 GetTilePos(int key)
    {
        int tx = key % (size.x * scale);
        int tz = key / (size.x * scale);

        float x = tx * 1.0f / scale - size.x * 0.5f + 0.5f / scale;
        float z = tz * 1.0f / scale - size.y * 0.5f + 0.5f / scale;
        return new Vector3(x, 0, z);
    }
    public bool ToggleTileContent(GameTileContentType type, Vector3 pos)
    {
        var key = GetTileKey(pos.x, pos.z);
        if (key < 0)
        {
            return false;
        }
        if (tiles.TryGetValue(key, out var tile))
        {
            if (tile.Content.Type != type)
            {
                tile.Content = tileContentFactory.Get(type);
                tile.Content.transform.localScale = new Vector3(1.0f / scale, 1.0f / scale, 1.0f / scale);
            }
            else
            {
                tile.Content.Recycle();
                tiles.Remove(key);
            }
            return true;
        }
        else
        {
            tile = Instantiate(tilePrefab);
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = GetTilePos(key);
            tile.Content = tileContentFactory.Get(type);
            tile.Content.transform.localScale = new Vector3(1.0f / scale, 1.0f / scale, 1.0f / scale);
            tiles.Add(key, tile);
        }
        return false;
    }
}