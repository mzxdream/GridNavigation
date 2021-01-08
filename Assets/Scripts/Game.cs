using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    GameBoard board = default;

    void Awake()
    {
        board.Init();
    }
    void Update()
    {
    }
}