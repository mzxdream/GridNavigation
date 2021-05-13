using System.Collections.Generic;
using UnityEngine;

class Wall
{
    public GameObject asset;
}

class Destination
{
    public int index;
    public GameObject asset;
}

class Character
{
    public GameObject asset;
    public int navAgentID;
    public float radius;
}

public class Game : MonoBehaviour
{

    [SerializeField, Range(0.1f, 1.0f)]
    float squareSize = 0.2f;
    [SerializeField]
    Texture2D squareTexture = default;
    [SerializeField]
    Transform wallPrefab = default, redDestinationPrefab = default, blueDestinationPrefab = default;
    [SerializeField]
    Transform redCharacterPrefab = default, blueCharacterPrefab = default;
    [SerializeField]
    float mass = 1.0f;
    [SerializeField]
    float radius = 0.6f;
    [SerializeField]
    float maxSpeed = 2.0f;

    Dictionary<int, Wall> walls;
    List<Character> redCharacters;
    List<Character> blueCharacters;
    Destination redDestination;
    Destination blueDestination;
    GridNavMesh navMesh;
    GridNavManager navManager;

    void Awake()
    {
        var xsize = (int)(transform.localScale.x / squareSize);
        var zsize = (int)(transform.localScale.z / squareSize);
        //transform.localScale = new Vector3(xsize * squareSize, 0.1f, zsize * squareSize);
        var material = this.GetComponent<MeshRenderer>().material;
        material.mainTexture = squareTexture;
        material.SetTextureScale("_MainTex", new Vector2(xsize, zsize));

        walls = new Dictionary<int, Wall>();
        redCharacters = new List<Character>();
        blueCharacters = new List<Character>();
        redDestination = new Destination
        {
            index = 0,
            asset = GameObject.Instantiate(redDestinationPrefab).gameObject,
        };
        blueDestination = new Destination
        {
            index = 1,
            asset = GameObject.Instantiate(blueDestinationPrefab).gameObject,
        };
        navMesh = new GridNavMesh();
        navMesh.Init(transform.position - new Vector3(xsize * squareSize * 0.5f, 0, zsize * squareSize * 0.5f), xsize, zsize, squareSize);
        navManager = new GridNavManager();
        navManager.Init(navMesh);
        redDestination.asset.transform.position = navMesh.GetSquarePos(redDestination.index);
        blueDestination.asset.transform.position = navMesh.GetSquarePos(blueDestination.index);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddRedCharacter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddBlueCharacter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetRedDesitination();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetBlueDestination();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            AddWall();
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            RemoveObject();
        }
        navManager.Update(Time.deltaTime);
        foreach (var c in redCharacters)
        {
            if (!navManager.GetLocation(c.navAgentID, out var pos, out var forward))
            {
                continue;
            }
            c.asset.transform.position = pos;
            c.asset.transform.forward = forward;
        }
        foreach (var c in blueCharacters)
        {
            if (!navManager.GetLocation(c.navAgentID, out var pos, out var forward))
            {
                continue;
            }
            c.asset.transform.position = pos;
            c.asset.transform.forward = forward;
        }
    }
    void OnDrawGizmos()
    {
        if (redCharacters != null)
        {
            foreach (var c in redCharacters)
            {
                DrawCharacterDetail(c, Color.red);
            }
        }
        if (blueCharacters != null)
        {
            foreach (var c in blueCharacters)
            {
                DrawCharacterDetail(c, Color.blue);
            }
        }
    }
    void DrawCharacterDetail(Character c, Color color)
    {
        Gizmos.color = color;
        navMesh.ClampInBounds(c.asset.transform.position, out var index, out var pos);
        pos = navMesh.GetSquarePos(index);

        int unitSize = (int)(c.radius * 2 / navMesh.SquareSize - 1e-4f) + 1;
        if ((unitSize & 1) == 0)
        {
            unitSize++;
        }
        Gizmos.DrawCube(pos, new Vector3(unitSize * navMesh.SquareSize, 0.1f, unitSize * navMesh.SquareSize));

        var p1 = c.asset.transform.position + Vector3.up;
        var prefVelocity = navManager.GetPrefVelocity(c.navAgentID);
        if (prefVelocity.sqrMagnitude >= 1e-4f)
        {
            var p2 = p1 + prefVelocity;
            UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.black, null, 5);
        }
        var velocity = navManager.GetVelocity(c.navAgentID);
        if (velocity.sqrMagnitude >= 1e-5f)
        {
            var p2 = p1 + velocity;
            UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.white, null, 5);
        }
    }
    void AddRedCharacter()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        var asset = GameObject.Instantiate(redCharacterPrefab).gameObject;
        asset.transform.position = hit.point;
        asset.transform.forward = Vector3.forward;
        asset.transform.localScale = new Vector3(radius * 2.0f, 0.5f, radius * 2.0f);
        var param = new GridNavAgentParam
        {
            mass = mass,
            radius = radius,
            maxSpeed = maxSpeed,
        };
        var navAgentID = navManager.AddAgent(asset.transform.position, asset.transform.forward, param);
        var c = new Character { asset = asset, navAgentID = navAgentID, radius = radius };
        redCharacters.Add(c);
    }
    void AddBlueCharacter()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        var asset = GameObject.Instantiate(blueCharacterPrefab).gameObject;
        asset.transform.position = hit.point;
        asset.transform.forward = Vector3.forward;
        asset.transform.localScale = new Vector3(radius * 2.0f, 0.5f, radius * 2.0f);
        var param = new GridNavAgentParam
        {
            mass = mass,
            radius = radius,
            maxSpeed = maxSpeed,
        };
        var navAgentID = navManager.AddAgent(asset.transform.position, asset.transform.forward, param);
        var c = new Character { asset = asset, navAgentID = navAgentID, radius = radius };
        blueCharacters.Add(c);
    }
    void AddWall()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        var index = navMesh.GetSquareIndex(hit.point);
        if (navMesh.IsSquareBlocked(index))
        {
            return;
        }
        navMesh.SetSquare(index, 1.0f, true);
        var asset = GameObject.Instantiate(wallPrefab).gameObject;
        asset.transform.position = navMesh.GetSquarePos(index);
        asset.transform.localScale = new Vector3(navMesh.SquareSize, 0.2f, navMesh.SquareSize);
        var wall = new Wall { asset = asset };
        walls.Add(index, wall);
    }
    void SetRedDesitination()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        navMesh.ClampInBounds(hit.point, out var index, out var pos);
        redDestination.index = index;
        redDestination.asset.transform.position = pos;
        foreach (var c in redCharacters)
        {
            navManager.StartMoving(c.navAgentID, pos);
        }
    }
    void SetBlueDestination()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        navMesh.ClampInBounds(hit.point, out var index, out var pos);
        blueDestination.index = index;
        blueDestination.asset.transform.position = pos;
        foreach (var c in blueCharacters)
        {
            navManager.StartMoving(c.navAgentID, pos);
        }
    }
    void RemoveObject()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        var index = navMesh.GetSquareIndex(hit.point);
        if (walls.TryGetValue(index, out var wall))
        {
            navMesh.SetSquare(index, 1.0f, false);
            Destroy(wall.asset);
            walls.Remove(index);
        }
        foreach (var c in redCharacters)
        {
            if ((c.asset.transform.position - hit.point).sqrMagnitude <= c.radius * c.radius)
            {
                navManager.RemoveAgent(c.navAgentID);
                Destroy(c.asset);
                redCharacters.Remove(c);
                break;
            }
        }
        foreach (var c in blueCharacters)
        {
            if ((c.asset.transform.position - hit.point).sqrMagnitude <= c.radius * c.radius)
            {
                navManager.RemoveAgent(c.navAgentID);
                Destroy(c.asset);
                blueCharacters.Remove(c);
                break;
            }
        }
    }
}