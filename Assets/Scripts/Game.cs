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
    public float radius;
}

public class Game : MonoBehaviour
{
    [SerializeField, Range(0.1f, 5.0f)]
    float squareSize = 0.2f;
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
    bool showGrid = false;
    [SerializeField]
    bool showCharacter = true;
    [SerializeField]
    bool showPath = false;

    List<Mesh> gridMeshs = null;
    List<Character> redCharacters;
    List<Character> blueCharacters;
    Destination redDestination;
    Destination blueDestination;
    NavMap navMap;
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
    void UpdateMap()
    {
        //meshObjs = new List<MeshObj>();
        //CollectMeshs();
        //var bmin = navMap.BMin;
        //for (int z = 0; z <= navMap.ZSize; z++)
        //{
        //    for (int x = 0; x <= navMap.XSize; x++)
        //    {
        //        var p = bmin + new Vector3(x * squareSize, 0, z * squareSize);
        //        GetPositionHeightAndType(p, out var h, out var areaType);
        //        navMap.SetCornerHeight(x, z, h);
        //    }
        //}
        //for (int z = 0; z < navMap.ZSize; z++)
        //{
        //    for (int x = 0; x < navMap.XSize; x++)
        //    {
        //        var p = bmin + new Vector3((x + 0.5f) * squareSize, 0, (z + 0.5f) * squareSize);
        //        GetPositionHeightAndType(p, out var h, out var areaType);
        //        navMap.SetSquareType(x, z, areaType);
        //    }
        //}
        //navMap.UpdateHeightMap();
        //gridMeshs = new List<Mesh>();
        //var verts = new List<Vector3>();
        //var tris = new List<int>();
        //var maxSlope = 0.0f;
        //for (int z = 0; z < navMap.ZSize; z++)
        //{
        //    for (int x = 0; x < navMap.XSize; x++)
        //    {
        //        if (verts.Count > 50000)
        //        {
        //            var gridMesh = new Mesh { vertices = verts.ToArray(), triangles = tris.ToArray() };
        //            gridMesh.RecalculateNormals();
        //            gridMeshs.Add(gridMesh);
        //            verts.Clear();
        //            tris.Clear();
        //        }
        //        var pTL = navMap.GetSquareCornerPos(x, z) + new Vector3(0, 0.001f, 0);
        //        var PTR = navMap.GetSquareCornerPos(x + 1, z) + new Vector3(0, 0.001f, 0);
        //        var pBL = navMap.GetSquareCornerPos(x, z + 1) + new Vector3(0, 0.001f, 0);
        //        var pBR = navMap.GetSquareCornerPos(x + 1, z + 1) + new Vector3(0, 0.001f, 0);

        //        var index = verts.Count;
        //        verts.Add(pBL);
        //        verts.Add(pTL);
        //        verts.Add(PTR);
        //        verts.Add(pBR);
        //        tris.Add(index);
        //        tris.Add(index + 1);
        //        tris.Add(index + 2);
        //        tris.Add(index + 2);
        //        tris.Add(index + 3);
        //        tris.Add(index);
        //    }
        //}
        //if (tris.Count > 0)
        //{
        //    var gridMesh = new Mesh { vertices = verts.ToArray(), triangles = tris.ToArray() };
        //    gridMesh.RecalculateNormals();
        //    gridMeshs.Add(gridMesh);
        //}
        //Debug.Log("max slope is " + maxSlope);
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
        if (showGrid && gridMeshs != null)
        {
            Gizmos.color = Color.green;
            foreach (var mesh in gridMeshs)
            {
                Gizmos.DrawWireMesh(mesh);
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
    void AddWall()
    {
        //var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        //{
        //    return;
        //}
        //var index = navMap.GetSquareIndex(hit.point);
        //if (walls.ContainsKey(index))
        //{
        //    return;
        //}
        //var asset = GameObject.Instantiate(wallPrefab).gameObject;
        //asset.transform.position = navMap.GetSquarePos(index);
        //asset.transform.localScale = new Vector3(navMap.SquareSize, 0.2f, navMap.SquareSize);
        //var wall = new Wall { asset = asset };
        //walls.Add(index, wall);
        // todo
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
        //var index = navMap.GetSquareIndex(hit.point);
        //if (walls.TryGetValue(index, out var wall))
        //{
        //    Destroy(wall.asset);
        //    walls.Remove(index);
        //    // todo
        //}
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