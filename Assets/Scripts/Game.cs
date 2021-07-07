using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GridNav;

class Destination
{
    public int x;
    public int z;
    public GameObject asset;
}

class Character
{
    public GameObject asset;
    public int navAgentID;
}

public class Game : MonoBehaviour
{
    [SerializeField]
    Transform redDestinationPrefab = default, blueDestinationPrefab = default;
    [SerializeField]
    Transform redCharacterPrefab = default, blueCharacterPrefab = default;
    [SerializeField, Range(0, 3)]
    int moveType = 0;
    [SerializeField]
    float mass = 1.0f;
    [SerializeField]
    float maxSpeed = 2.0f;
    [SerializeField]
    bool showCharacter = true;
    [SerializeField]
    bool showPath = false;

    List<Character> redCharacters;
    List<Character> blueCharacters;
    Destination redDestination;
    Destination blueDestination;
    NavManager navManager;
    float lastTime;

    void Awake()
    {
        redCharacters = new List<Character>();
        blueCharacters = new List<Character>();
        redDestination = new Destination
        {
            x = 0,
            z = 0,
            asset = GameObject.Instantiate(redDestinationPrefab).gameObject,
        };
        blueDestination = new Destination
        {
            x = 1,
            z = 1,
            asset = GameObject.Instantiate(blueDestinationPrefab).gameObject,
        };
        var navDataPath = "Assets/Config/navData.asset";
        var navData = AssetDatabase.LoadAssetAtPath<GridNavScriptableObject>(navDataPath);
        if (navData == null)
        {
            Debug.LogError("nav data not exists");
            return;
        }

        //navMap = new NavMap();
        //var xsize = (int)(transform.localScale.x / squareSize);
        //var zsize = (int)(transform.localScale.z / squareSize);
        //navMap.Init(transform.position - new Vector3(xsize * squareSize * 0.5f, 0, zsize * squareSize * 0.5f), xsize, zsize, squareSize);
        //UpdateMap();
        //navManager = new NavManager();
        //navManager.Init(navMap);
        //navManager.GetMoveDef(0).SetUnitSize(4);
        //navManager.GetMoveDef(1).SetUnitSize(6);
        //navManager.GetMoveDef(2).SetUnitSize(8);
        //navManager.GetMoveDef(3).SetUnitSize(10);
        //navManager.AfterInit();
        //redDestination.asset.transform.position = navMap.GetSquarePos(redDestination.x, redDestination.z);
        //blueDestination.asset.transform.position = navMap.GetSquarePos(blueDestination.x, blueDestination.z);
        lastTime = Time.realtimeSinceStartup;
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
        foreach (var c in redCharacters)
        {
            if (!navManager.GetLocation(c.navAgentID, out var pos, out var forward))
            {
                continue;
            }
            c.asset.transform.position = pos;
            if (forward != Vector3.zero)
            {
                c.asset.transform.forward = forward;
            }
        }
        foreach (var c in blueCharacters)
        {
            if (!navManager.GetLocation(c.navAgentID, out var pos, out var forward))
            {
                continue;
            }
            c.asset.transform.position = pos;
            if (forward != Vector3.zero)
            {
                c.asset.transform.forward = forward;
            }
        }
        var nowTime = Time.realtimeSinceStartup;
        if (nowTime - lastTime > 0.0333f)
        {
            navManager.Update();
            lastTime = Time.realtimeSinceStartup;
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
        var agent = navManager.GetAgent(c.navAgentID);
        if (agent == null)
        {
            return;
        }
        if (showCharacter)
        {
            int unitSize = agent.moveDef.GetUnitSize();
            var pos = navMap.GetSquareCornerPos(agent.mapPos.x, agent.mapPos.y);
            pos.x += unitSize * navMap.SquareSize * 0.5f;
            pos.z += unitSize * navMap.SquareSize * 0.5f;
            Gizmos.color = color;
            Gizmos.DrawCube(pos, new Vector3(unitSize * navMap.SquareSize, 0.1f, unitSize * navMap.SquareSize));
            {
                var p1 = c.asset.transform.position + Vector3.up;
                var prefVelocity = agent.prefVelocity;
                if (prefVelocity.sqrMagnitude >= 1e-5f)
                {
                    var p2 = p1 + prefVelocity.normalized * 2.0f;
                    UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.blue, null, 5);
                }
                var velocity = agent.velocity;
                if (velocity.sqrMagnitude >= 1e-5f)
                {
                    var p2 = p1 + velocity.normalized * 2.0f;
                    UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.white, null, 5);
                }
            }
        }
        if (showPath && agent.path != null && agent.path.Count > 0)
        {
            var p1 = agent.path[agent.path.Count - 1] + Vector3.up;
            for (int i = agent.path.Count - 2, j = 0; i >= 0 && j <= 10; i--, j++)
            {
                var p2 = agent.path[i] + Vector3.up;
                UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.yellow, null, 5);
                p1 = p2;
            }
        }
    }
    Character CreateCharacter(Transform prefab)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return null;
        }
        var param = new NavAgentParam
        {
            moveType = moveType,
            mass = mass,
            maxSpeed = maxSpeed,
            isPushResistant = true,
        };
        var navAgentID = navManager.AddAgent(hit.point, param);
        var agent = navManager.GetAgent(navAgentID);
        var radius = agent.radius;

        var asset = GameObject.Instantiate(prefab).gameObject;
        asset.transform.position = hit.point;
        asset.transform.forward = Vector3.forward;
        asset.transform.localScale = new Vector3(radius * 2.0f, 0.5f, radius * 2.0f);
        var c = new Character { asset = asset, navAgentID = navAgentID, radius = radius };
        return c;
    }
    void AddRedCharacter()
    {
        var c = CreateCharacter(redCharacterPrefab);
        if (c != null)
        {
            redCharacters.Add(c);
        }
    }
    void AddBlueCharacter()
    {
        var c = CreateCharacter(blueCharacterPrefab);
        if (c != null)
        {
            blueCharacters.Add(c);
        }
    }
    void SetRedDesitination()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return;
        }
        navMap.ClampInBounds(hit.point, out var x, out var z, out var pos);
        redDestination.x = x;
        redDestination.z = z;
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
        navMap.ClampInBounds(hit.point, out var x, out var z, out var pos);
        blueDestination.x = x;
        blueDestination.z = z;
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