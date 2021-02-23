using UnityEngine;

public enum CharacterType { Red, Blue }

public class Character
{
    private readonly CharacterContent content;
    private readonly CharacterType type;
    private GridMoveAgent moveAgent;

    public CharacterType Type { get => type; }

    public Character(CharacterContent content, CharacterType type, Vector3 position, Vector3 forward, GridMoveManager moveManager)
    {
        this.content = content;
        this.type = type;
        content.transform.position = position;
        content.transform.forward = forward;
        content.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        var param = new GridMoveAgentParam
        {
            teamID = 1,
            unitSize = 3,
            mass = 1.0f,
            maxSpeed = 1.0f,
            isPushResistant = true,
        };
        moveAgent = moveManager.CreateAgent(position, forward, param);
    }
    public void Clear()
    {
        content.Clear();
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
        content.SetPosition(moveAgent.Pos);
        content.SetForward(moveAgent.Forward);
    }
}