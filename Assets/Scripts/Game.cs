using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GridNav;

class Destination
{
    public int teamID;
    public GameObject asset;
}

class Character
{
    public int teamID;
    public GameObject asset;
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
    int moveType = 0;
    [SerializeField]
    int teamID = 0;
    [SerializeField]
    float mass = 1.0f;
    [SerializeField]
    float maxSpeed = 2.0f;
    [SerializeField]
    bool isPushResistant = true;
    [SerializeField]
    bool showSquares = true;
    [SerializeField]
    bool showPath = false;

    List<Destination> destinations;
    List<Character> characters;
    NavManager navManager;
    float lastTime;

    void Awake()
    {
        destinations = new List<Destination>();
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
            moveDef.SetSpeedModMult(NavSpeedModMultType.Idle, moveData.speedModMultIdle);
            moveDef.SetSpeedModMult(NavSpeedModMultType.Busy, moveData.speedModMultBusy);
            moveDef.SetSpeedModMult(NavSpeedModMultType.Moving, moveData.speedModMultMoving);
            moveDef.SetSpeedModMult(NavSpeedModMultType.Blocked, moveData.speedModMultBlocked);
        }
        navManager = new NavManager();
        if (!navManager.Init(navMap, moveDefs))
        {
            Debug.LogError("init nav manager failed");
            navManager = null;
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
        if (characters != null)
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
    }
    bool TryGetTeamColor(int teamID, out Color color)
    {
        color = Color.white;
        if (teams == null)
        {
            return false;
        }
        foreach (var team in teams)
        {
            if (team.id == teamID)
            {
                color = team.color;
                return true;
            }
        }
        return false;
    }
    void AddCharacter()
    {
        if (navManager == null)
        {
            Debug.LogError("nav manager is null");
            return;
        }
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            Debug.LogError("get position failed");
            return;
        }
        if (!TryGetTeamColor(teamID, out var teamColor))
        {
            Debug.LogError("get team color failed");
            return;
        }
        var moveDef = navManager.GetMoveDef(moveType);
        if (moveDef == null)
        {
            Debug.LogError("moveType:" + moveType + " not exists");
            return;
        }
        var navParam = new NavAgentParam
        {
            moveType = moveType,
            teamID = teamID,
            mass = mass,
            maxSpeed = maxSpeed,
            isPushResistant = isPushResistant,
        };
        var navAgentID = navManager.AddAgent(hit.point, navParam);
        if (navAgentID == 0)
        {
            Debug.LogError("add agent failed");
            return;
        }
        var navAgent = navManager.GetAgent(navAgentID);
        var asset = GameObject.Instantiate(characterPrefab).gameObject;
        asset.transform.position = navAgent.pos;
        asset.transform.forward = Vector3.forward;
        asset.transform.localScale = new Vector3(navAgent.radius * 2.0f, 0.5f, navAgent.radius * 2.0f);
        var body = asset.transform.Find("Model").Find("Body").gameObject;
        body.GetComponent<Renderer>().material.SetColor("_Color", teamColor);
        var c = new Character { teamID = teamID, asset = asset, navAgentID = navAgentID };
        characters.Add(c);
    }
    void SetDesitination()
    {
        if (navManager == null)
        {
            Debug.LogError("nav manager is null");
            return;
        }
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            Debug.LogError("get position failed");
            return;
        }
        if (!TryGetTeamColor(teamID, out var teamColor))
        {
            Debug.LogError("team not exists:" + teamID);
            return;
        }
        var navMap = navManager.GetNavMap();
        navMap.ClampInBounds(hit.point, out _, out _, out var pos);

        var destination = destinations.Find((d) => { return d.teamID == teamID; });
        if (destination == null)
        {
            var asset = GameObject.Instantiate(destinationPrefab).gameObject;
            var body = asset.transform.Find("Model").Find("Body").gameObject;
            body.GetComponent<Renderer>().material.SetColor("_Color", teamColor);
            destination = new Destination
            {
                teamID = teamID,
                asset = asset,
            };
            destinations.Add(destination);
        }
        destination.asset.transform.position = pos;
        foreach (var c in characters)
        {
            if (c.teamID != teamID)
            {
                continue;
            }
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
        foreach (var c in characters)
        {
            var navAgent = navManager.GetAgent(c.navAgentID);
            var radius = navAgent.radius;
            if ((c.asset.transform.position - hit.point).sqrMagnitude <= radius * radius)
            {
                navManager.RemoveAgent(c.navAgentID);
                Destroy(c.asset);
                characters.Remove(c);
                break;
            }
        }
    }
}