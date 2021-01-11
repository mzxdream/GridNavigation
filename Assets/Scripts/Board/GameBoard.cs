using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [SerializeField]
    Vector2Int size = new Vector2Int(11, 11);
    [SerializeField]
    Transform ground = default;
    [SerializeField]
    Texture2D gridTexture = default;
    [SerializeField]
    GameTile tilePrefab = default;
    [SerializeField]
    GameTileContentFactory tileContentFactory = default;
    GameTile[] tiles;

    public bool Init()
    {
        ground.localScale = new Vector3(size.x, size.y, 1f);
        var material = ground.GetComponent<MeshRenderer>().material;
        material.mainTexture = gridTexture;
        material.SetTextureScale("_MainTex", size);

        var offset = new Vector2((size.x - 1) * 0.5f, (size.y - 1) * 0.5f);
        tiles = new GameTile[size.x * size.y];
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                var tile = Instantiate(tilePrefab);
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = new Vector3(x - offset.x, 0, y - offset.y);
                tile.Content = tileContentFactory.Get(GameTileContentType.Empty);
                tiles[x + y * size.x] = tile;
            }
        }
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
    public GameTile GetTile(float x, float z)
    {
        var tx = (int)(x + size.x * 0.5f);
        var ty = (int)(z + size.y * 0.5f);
        if (tx >= 0 && tx < size.x && ty >= 0 && ty < size.y)
        {
            return tiles[tx + ty * size.y];
        }
        return null;
    }
    public bool ChangeTileContent(float x, float z, GameTileContentType type)
    {
        var tile = GetTile(x, z);
        if (tile)
        {
            tile.Content = tileContentFactory.Get(type);
            return true;
        }
        return false;
    }
}