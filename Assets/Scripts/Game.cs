using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
public class MoveDefData
{
    public int unitSize = 4;
    public float maxAngle = 60.0f;
    public float slopeMod = 0.0f;
    public float speedModWalkable = 1.0f;
    [ReadOnly]
    public float speedModUnwalkable = 0.0f;
    public float speedModJump = 2.0f;
    public float speedModMultIdle = 0.35f;
    public float speedModMultBusy = 0.10f;
    public float speedModMultMoving = 0.65f;
    public float speedModMultBlocked = 0.01f;
}

public class Game : MonoBehaviour
{
    [SerializeField]
    Transform destinationPrefab = default;
    [SerializeField]
    Transform characterPrefab = default;
    [SerializeField]
    MoveDefData[] moveDefDatas = default;
    //UI
    Toggle showSquaresToggle;
    InputField showPathCountInputField;
    int createType = 0;
    List<Toggle> createToggles;
    InputField moveTypeInputField, massInputField, maxSpeedInputField;
    Toggle pushResistantToggle;
    //
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
        navManager = new NavManager();
        if (!navManager.Init(navMap, 1024, moveDefDatas.Length))
        {
            Debug.LogError("init nav manager failed");
            navManager = null;
            return;
        }
        for (int i = 0; i < moveDefDatas.Length; i++)
        {
            var moveData = moveDefDatas[i];
            var moveDef = navManager.GetMoveDef(i);
            moveDef.SetUnitSize(moveData.unitSize);
            moveDef.SetMaxSlope(NavUtils.DegreesToSlope(moveData.maxAngle));
            moveDef.SetSlopeMod(moveData.slopeMod);
            moveDef.SetSpeedMod(0, moveData.speedModWalkable);
            moveDef.SetSpeedMod(1, moveData.speedModUnwalkable);
            moveDef.SetSpeedMod(2, moveData.speedModJump);
            moveDef.SetSpeedModMult(NavSpeedModMultType.Idle, moveData.speedModMultIdle);
            moveDef.SetSpeedModMult(NavSpeedModMultType.Busy, moveData.speedModMultBusy);
            moveDef.SetSpeedModMult(NavSpeedModMultType.Moving, moveData.speedModMultMoving);
            moveDef.SetSpeedModMult(NavSpeedModMultType.Blocked, moveData.speedModMultBlocked);
        }
        lastTime = Time.realtimeSinceStartup;
        InitUI();
    }
    void CheckInput()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            RemoveObject();
        }
        if (Input.GetKeyDown(KeyCode.Q) && createToggles.Count > 0)
        {
            createToggles[(createType + 1) % createToggles.Count].isOn = true;
        }
        int teamID = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            teamID = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            teamID = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            teamID = 3;
        }
        if (teamID != -1)
        {
            if (createType == 0)
            {
                AddCharacter(teamID);
            }
            else if (createType == 1) 
            {
                SetDesitination(teamID);
            }
        }
    }
    void Update()
    {
        CheckInput();
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
        DrawNavMap();
        DrawCharacters();
    }
    void DrawNavMap()
    {
        var areaMeshs = GridNavMapShow.Instance.AreaMeshes;
        if (areaMeshs == null)
        {
            return;
        }
        foreach (var v in areaMeshs)
        {
            var areaColor = GetAreaColor(v.Key);
            foreach (var mesh in v.Value)
            {
                Gizmos.color = areaColor;
                Gizmos.DrawMesh(mesh);
                Gizmos.color = Color.black;
                Gizmos.DrawWireMesh(mesh);
            }
        }
    }
    void DrawCharacters()
    {
        if (characters == null)
        {
            return;
        }
        var showSquares = showSquaresToggle.isOn;
        int showPathCount = 0;
        int.TryParse(showPathCountInputField.text, out showPathCount);
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
            if (showPathCount > 0 && navAgent.path != null && navAgent.path.Count >= 2)
            {
                var p1 = navAgent.path[navAgent.path.Count - 1] + Vector3.up;
                for (int i = navAgent.path.Count - 2, j = 0; i >= 0 && j < showPathCount; i--, j++)
                {
                    var p2 = navAgent.path[i] + Vector3.up;
                    UnityEditor.Handles.DrawBezier(p1, p2, p1, p2, Color.yellow, null, 5);
                    p1 = p2;
                }
            }
        }
    }
    void AddCharacter(int teamID)
    {
        if (navManager == null)
        {
            Debug.LogError("nav manager is null");
            return;
        }
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            //Debug.LogError("get position failed");
            return;
        }
        if (!int.TryParse(moveTypeInputField.text, out var moveType))
        {
            Debug.LogError("parse moveType failed");
            return;
        }
        if (!float.TryParse(massInputField.text, out var mass))
        {
            Debug.LogError("parse mass failed");
            return;
        }
        if (mass < 1e-4f)
        {
            Debug.LogError("mass is less then 1e-4f");
            return;
        }
        if (!float.TryParse(maxSpeedInputField.text, out var maxSpeed))
        {
            Debug.LogError("parse max speed failed");
            return;
        }
        var pushResistant = pushResistantToggle.isOn;

        var teamColor = GetTeamColor(teamID);
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
            isPushResistant = pushResistant,
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
    void SetDesitination(int teamID)
    {
        if (navManager == null)
        {
            Debug.LogError("nav manager is null");
            return;
        }
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            //Debug.LogError("get position failed");
            return;
        }
        var teamColor = GetTeamColor(teamID);
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
    private void InitUI()
    {
        showSquaresToggle = transform.Find("Canvas/GridNavigation/ShowSquaresToggle").GetComponent<Toggle>();
        showPathCountInputField = transform.Find("Canvas/GridNavigation/ShowPathCountInputField").GetComponent<InputField>();
        createToggles = new List<Toggle>();
        createToggles.Add(transform.Find("Canvas/GridNavigation/CharacterToggle").GetComponent<Toggle>());
        createToggles.Add(transform.Find("Canvas/GridNavigation/DestinationToggle").GetComponent<Toggle>());
        Action<Toggle, int> onToggleValueChange = (Toggle t, int index) =>
        {
            if (t.isOn)
            {
                createType = index;
            }
        };
        for (int i = 0; i < createToggles.Count; i++)
        {
            var toggle = createToggles[i];
            if (toggle.isOn)
            {
                createType = i;
            }
            int j = i;
            toggle.onValueChanged.AddListener(delegate { onToggleValueChange(toggle, j); });
        }
        moveTypeInputField = transform.Find("Canvas/GridNavigation/MoveTypeInputField").GetComponent<InputField>();
        massInputField = transform.Find("Canvas/GridNavigation/MassInputField").GetComponent<InputField>();
        maxSpeedInputField = transform.Find("Canvas/GridNavigation/MaxSpeedInputField").GetComponent<InputField>();
        pushResistantToggle = transform.Find("Canvas/GridNavigation/PushResistantToggle").GetComponent<Toggle>();
    }
    private static int Bit(int a, int b)
    {
        return (a & (1 << b)) >> b;
    }
    private static Color GetAreaColor(int i)
    {
        if (i == 0)
        {
            return new Color(0f, 0.75f, 1f, 0.5f);
        }
        int r = (Bit(i, 4) + Bit(i, 1) * 2 + 1) * 63;
        int g = (Bit(i, 3) + Bit(i, 2) * 2 + 1) * 63;
        int b = (Bit(i, 5) + Bit(i, 0) * 2 + 1) * 63;
        return new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, 0.5f);
    }
    private static Color GetTeamColor(int teamID)
    {
        if (teamID == 2)
        {
            return Color.blue;
        }
        else if (teamID == 3)
        {
            return Color.yellow;
        }
        return Color.red;
    }
}