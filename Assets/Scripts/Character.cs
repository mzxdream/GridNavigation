using UnityEngine;

public enum CharacterType { Red, Blue }

public class Character
{
    private readonly CharacterType type;
    private readonly CharacterContent content;
    private GridMoveAgent moveAgent;

    public CharacterType Type { get => type; }

    public Character(Game game, CharacterType type, Vector3 position, Vector3 forward, float radius)
    {
        this.type = type;
        //content = ;
        content.transform.position = position;
        content.transform.forward = forward;
        content.transform.localScale = new Vector3(radius, radius, radius);
        var param = new GridMoveAgentParam
        {
            teamID = 1,
            unitSize = 3,
            mass = 1.0f,
            maxSpeed = 1.0f,
            isPushResistant = true,
        };
        //moveAgent = moveManager.CreateAgent(position, forward, param);
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