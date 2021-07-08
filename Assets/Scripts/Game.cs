using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GridNav;

class Destination
{
    public GameObject asset;
}

class Character
{
    public GameObject asset;
    public NavAgentParam navParam;
    public int navAgentID;
}

[Serializable]
public class Team
{
    public int id;
    public Color color;
}

public class Game : MonoBehaviour
{
    [SerializeField]
    Transform destinationPrefab = default;
    [SerializeField]
    Transform characterPrefab = default;
    [SerializeField]
    Team[] teams = default;
    [SerializeField]
    int teamID = 0;
    [SerializeField]
    int moveType = 0;
    [SerializeField]
    float mass = 1.0f;
    [SerializeField]
    float maxSpeed = 2.0f;
    [SerializeField]
    bool showSquares = true;
    [SerializeField]
    bool showPath = false;

    Dictionary<int, Destination> destinations;
    List<Character> characters;
    NavManager navManager;
    float lastTime;

    void Awake()
    {
        destinations = new Dictionary<int, Destination>();
        characters = new List<Character>();

        var navDataPath = "Assets/Config/navData.asset";
        var navData = AssetDatabase.LoadAssetAtPath<GridNavScriptableObject>(navDataPath);
        if (navData == null)
        {
            Debug.LogError("nav data not exists");
            return;
        }
        var navMap = new NavMap();
        if (!navMap.Init(navData.bmin, navData.xsize, navData.zsize, navData.squareSize, navData.squareTypeMap, navData.cornerHeightMap))
        {
            Debug.LogError("Init map failed");
            return;
        }
        var moveDefs = new NavMoveDef[navData.moveDefDatas.Length];
        for (int i = 0; i < navData.moveDefDatas.Length; i++)
        {
            var moveData = navData.moveDefDatas[i];
            var moveDef = new NavMoveDef();
            moveDef.SetUnitSize(moveData.unitSize);
            moveDef.SetMaxSlope(moveData.maxSlope);
            moveDef.SetSlopeMod(moveData.slopeMod);
            for (int j = 0; j < moveData.speedMods.Length; j++)
            {
                moveDef.SetSpeedMod(j, moveData.speedMods[j]);
            }
            for (int j = 0; j < moveData.speedModMults.Length; j++)
            {
                moveDef.SetSpeedModMult((NavSpeedModMultType)j, moveData.speedModMults[j]);
            }
        }
        navManager = new NavManager();
        if (!navManager.Init(navMap, moveDefs))
        {
            Debug.LogError("init nav manager failed");
            return;
        }
        lastTime = Time.realtimeSinceStartup;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddCharacter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetDesitination();
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            RemoveObject();
        }
        foreach (var c in characters)
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
        foreach (var c in characters)
        {
            var navAgent = navManager.GetAgent(c.navAgentID);
            if (navAgent == null)
            {
                continue;
            }
            {
                var p1 = c.asset.transform.position + Vector3.up;
                var velocity = navAgent.velocity;
                if (velocity.sqrMagnitude >= 1e-5f)
                {
                    var p2 = p1 + velocity.normalized * 2.0f;
                    UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.black, null, 5);
                }
                var prefVelocity = navAgent.prefVelocity;
                if (prefVelocity.sqrMagnitude >= 1e-5f)
                {
                    var p2 = p1 + prefVelocity.normalized * 2.0f;
                    UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.blue, null, 5);
                }
            }
            if (showSquares)
            {
                var navMap = navManager.GetNavMap();
                var squareSize = navMap.SquareSize;
                int unitSize = navAgent.moveDef.GetUnitSize();
                var pos = navMap.GetSquareCornerPos(navAgent.mapPos.x, navAgent.mapPos.y);
                pos += new Vector3(unitSize * squareSize * 0.5f, 0, unitSize * squareSize * 0.5f);
                Gizmos.color = Color.grey;
                Gizmos.DrawCube(pos, new Vector3(unitSize * navMap.SquareSize, 0.1f, unitSize * navMap.SquareSize));
            }
            if (showPath && navAgent.path != null && navAgent.path.Count > 0)
            {
                var p1 = navAgent.path[navAgent.path.Count - 1] + Vector3.up;
                var start = navAgent.path.Count - 2;
                var end = Mathf.Max(0, start - 10);
                while (start != end)
                {
                    var p2 = navAgent.path[start] + Vector3.up;
                    UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.yellow, null, 5);
                    p1 = p2;
                    start--;
                }
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