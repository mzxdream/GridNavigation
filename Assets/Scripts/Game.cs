using UnityEngine;

public enum OperatorType { AddRed, AddBlue, SetWall, SetDestination }
public class Game : MonoBehaviour
{
    [SerializeField]
    GameBoard board = default;
    [SerializeField]
    CharacterCollection characterCollection = default;
    static Game instance;
    public static Game Instance => instance;
    OperatorType operatorType;

    void Awake()
    {
        board.Init();
        characterCollection.Init();
    }
    void OnEnable()
    {
        instance = this;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            operatorType = OperatorType.AddRed;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            operatorType = OperatorType.AddBlue;
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
            case OperatorType.AddRed: characterCollection.AddCharacter(pos.x, pos.z, CharacterType.Red); break;
            case OperatorType.AddBlue: characterCollection.AddCharacter(pos.x, pos.z, CharacterType.Blue); break;
            case OperatorType.SetWall: board.ToggleTileContent(pos.x, pos.z, GameTileContentType.Wall); break;
            case OperatorType.SetDestination: board.ToggleTileContent(pos.x, pos.z, GameTileContentType.Destination); break;
        }
    }
}