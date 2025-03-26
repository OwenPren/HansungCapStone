using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssistantManager : MonoBehaviour
{
    public GameStartEventSO gameStartEvent;

    private void OnEnable()
    {
        gameStartEvent.OnGameStart += OnGameStart;
    }

    private void OnDisable()
    {
        gameStartEvent.OnGameStart -= OnGameStart;
    }

    private void OnGameStart()
    {
        StartThread();
    }

    private void StartThread()
    {
       
        Debug.Log("Starting thread Manager...");
       
    }
}
