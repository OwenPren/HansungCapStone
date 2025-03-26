using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameStartEventSO gameStartEvent;

    private void Start()
    {
        Debug.Log("Game Started");

        // 이벤트 발생 원하는 특정 시점에 실행
        gameStartEvent.Raise();
    }

}
