using UnityEngine;

public enum CharacterType { RedSmall, RedMedium, RedLarge, BlueSmall, BlueMedium, BlueLarge }

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
        var agentParams = new GridMoveAgentParams
        {
            allyID = 1,
            mass = 1.0f,
            xsize = 3,
            zsize = 3,
            speed = 1.0f,
            maxAcc = 0.2f,
            maxDec = 0.2f,
            turnRate = 1.0f,
            isPushResistant = true,
        };
        this.moveAgent = moveManager.AddAgent(pos - moveManager.Pos, forward, agentParams);
        return true;
    }
    public void Clear()
    {
        moveManager.RemoveAgent(moveAgent);
        moveAgent = null;
        originFactory.Reclaim(this);
    }
    public void MoveTo(Vector3 pos)
    {
        moveAgent.StartMoving(pos - moveManager.Pos, 0.01f);
    }
    public void Update()
    {
        transform.position = moveAgent.Pos + moveManager.Pos;
        transform.forward = moveAgent.FlatFrontDir;
    }
}