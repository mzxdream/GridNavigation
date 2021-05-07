using UnityEngine;

public enum CharacterType { Red, Blue }

public class Character
{
    private readonly CharacterAsset asset;
    private readonly CharacterType type;
    private readonly GridNavManager navManager;
    private int navAgentID;

    public CharacterType Type { get => type; }

    public Character(CharacterAsset prefab, CharacterType type, Vector3 position, Vector3 forward, float radius, GridNavManager navManager)
    {
        asset = GameObject.Instantiate(prefab);
        this.type = type;
        asset.SetPosition(position);
        asset.SetForward(forward);
        asset.SetScale(new Vector3(radius, radius, radius));
        var param = new GridNavAgentParam
        {
            mass = 1.0f,
            radius = 0.4f,
            maxSpeed = 1.0f,
            maxAcc = 1.0f,
            maxTurnAngle = 10.0f,
        };
        this.navManager = navManager;
        navAgentID = navManager.AddAgent(position, forward, param);
    }
    public void Clear()
    {
        asset.Clear();
    }
    public void StartMoving(Vector3 position)
    {
        navManager.StartMoving(navAgentID, position);
    }
    public void StopMoving()
    {
        //moveAgent.StopMoving(false, true);
    }
    public void Update()
    {
        if (navManager.GetLocation(navAgentID, out var pos, out var forward))
        {
            asset.SetPosition(pos);
            asset.SetForward(forward);
        }
    }
}