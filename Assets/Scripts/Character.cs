using UnityEngine;

public enum CharacterType { Red, Blue }

public class Character : MonoBehaviour
{
    [SerializeField]
    Transform model = default;
    [SerializeField]
    float radius = 0.6f;
    public float Radius { get => radius; }
    [SerializeField]
    float maxSpeed = 1.0f;
    public float MaxSpeed { get => maxSpeed; }
    GridMoveManager moveManager;
    GridMoveAgent moveAgent;
    CharacterFactory originFactory;
    public CharacterFactory OriginFactory
    {
        get => originFactory;
        set
        {
            Debug.Assert(originFactory == null, "redifined factory!");
            originFactory = value;
        }
    }
    public bool Init(Vector3 pos, Vector3 forward, GridMoveManager moveManager)
    {
        transform.position = pos;
        transform.forward = forward;
        model.localScale = new Vector3(radius, radius, radius);
        this.moveManager = moveManager;
        var agentParams = new GridMoveAgentParam
        {
            teamID = 1,
            unitSize = 3,
            mass = 1.0f,
            maxSpeed = 1.0f,
            isPushResistant = true,
        };
        this.moveAgent = moveManager.CreateAgent(pos, forward, agentParams);
        return true;
    }
    public void Clear()
    {
        moveAgent = null;
        originFactory.Reclaim(this);
    }
    public void MoveTo(Vector3 pos)
    {
        moveAgent.StartMoving(pos, 0.01f);
    }
    public void Update()
    {
        transform.position = moveAgent.Pos;
        transform.forward = moveAgent.Forward;
    }
}