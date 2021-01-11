using UnityEngine;

public enum OperatorType { AddCharacter, SetEmpty, SetWall, SetDestination }
public class Game : MonoBehaviour
{
    [SerializeField]
    GameBoard board = default;
    static Game instance;
    public static Game Instance => instance;
    OperatorType operatorType;

    void Awake()
    {
        board.Init();
    }
    void OnEnable()
    {
        instance = this;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            operatorType = OperatorType.AddCharacter;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            operatorType = OperatorType.SetEmpty;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            operatorType = OperatorType.SetWall;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            operatorType = OperatorType.SetDestination;
        }
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                OnMouseLeftDown(hit.point);
            }
        }
    }
    void OnMouseLeftDown(Vector3 pos)
    {
        switch (operatorType)
        {
            case OperatorType.SetEmpty: board.ChangeTileContent(pos.x, pos.z, GameTileContentType.Empty); break;
            case OperatorType.SetWall: board.ChangeTileContent(pos.x, pos.z, GameTileContentType.Wall); break;
            case OperatorType.SetDestination: board.ChangeTileContent(pos.x, pos.z, GameTileContentType.Destination); break;
        }
    }
}