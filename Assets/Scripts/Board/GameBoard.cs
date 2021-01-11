using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [SerializeField]
    Transform ground = default;
    [SerializeField]
    Vector2Int size;
    public void Init()
    {
    }
}