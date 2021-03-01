using UnityEngine;

public enum CharacterType { Red, Blue }

public class Character
{
    private readonly CharacterAsset asset;
    private readonly CharacterType type;
    private GridMoveAgent moveAgent;

    public CharacterType Type { get => type; }

    public Character(CharacterAsset prefab, CharacterType type, Vector3 position, Vector3 forward, float radius, GridMoveManager moveManager)
    {
        asset = GameObject.Instantiate(prefab);
        this.type = type;
        asset.SetPosition(position);
        asset.SetForward(forward);
        asset.SetScale(new Vector3(radius, radius, radius));
        var param = new GridMoveAgentParam
        {
            teamID = 1,
            radius = 0.6f,
            mass = 1.0f,
            maxSpeed = 1.0f,
            maxAcc = 1.0f,
            maxDec = 1.0f,
            isPushResistant = true,
        };
        moveAgent = moveManager.CreateAgent(position, forward, param);
    }
    public void Clear()
    {
        asset.Clear();
    }
    public void StartMoving(Vector3 position)
    {
        moveAgent.StartMoving(position, 0.01f);
    }
    public void StopMoving()
    {
        moveAgent.StopMoving(false, true);
    }
    public void Update()
    {
        asset.SetPosition(moveAgent.Pos);
        asset.SetForward(moveAgent.Forward);
    }
}