using UnityEngine;

public enum CharacterType { Red, Blue }

public class Character
{
    private readonly CharacterType type;
    private readonly CharacterContent content;
    private readonly GridMoveManager moveManager;
    private GridMoveAgent moveAgent;

    public CharacterType Type { get => type; }

    public Character(Vector3 position, Vector3 forward, CharacterType type, CharacterContentFactory contentFactory, GridMoveManager moveManager)
    {
        float radius = 0.6f;

        this.type = type;
        this.content = contentFactory.Get(type);
        this.content.SetPosition(position);
        this.content.SetForward(forward);
        this.content.SetScale(radius);
        this.moveManager = moveManager;
        var param = new GridMoveAgentParam
        {
            teamID = 1,
            unitSize = 3,
            mass = 1.0f,
            maxSpeed = 1.0f,
            isPushResistant = true,
        };
        this.moveAgent = moveManager.CreateAgent(position, forward, param);
    }
    public void Recycle()
    {
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