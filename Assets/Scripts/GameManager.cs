using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum GameState
{
    Waitng, // 게임 시작 대기중
    InProgress, // 라운드 진행중
    Started, // 라운드 시작
    Ended, // 라운드 종료

}

public class GameManager : NetworkBehaviour
{
    public float timeLimit = 60f;
    
    //Sriptable Object
    public GameStartEventSO gameStartEvent;
    public RoundStartEventSO roundStartEvent;
    public GameEndEventSO gameEndEvent;

    //Network Object
    [Networked] public GameState State { get; private set; }
    [Networked] public float Timer { get; private set; }
    [Networked] public int CurrentRound { get; private set; }

    private bool isWaiting = false;
    private float waitTimer = 0f;

    public override void FixedUpdateNetwork()
    {
        //클라이언트 제외
        if (!Runner.IsServer) return;

        switch (State)
        {
            case GameState.Waitng:
                // 게임 시작 전 대기 상태: 테스트용으로 10초 대기 후 StartGame() 호출
                if (!isWaiting)
                {
                     waitTimer = 0f;
                     isWaiting = true;
                }
                waitTimer += Runner.DeltaTime;
                if (waitTimer >= 10f)
                {
                     StartGame();
                     isWaiting = false;
                }
                break;

            case GameState.Started:
                StartRound();
                break;
            case GameState.InProgress:
                Timer -= Runner.DeltaTime;

                if (Timer <= 0f)
                {
                    EndRound();
                } 
                break;
        }
    }

    void StartGame()
    {
        //게임 시작 로직 구현 필요
        Debug.Log("StartGame function");
        State = GameState.Started;
    }

    void StartRound()
    {
        CurrentRound++;
        Debug.Log("[Round] " + CurrentRound + " Started");
        if (CurrentRound > 12)
        {
             Debug.Log("No more rounds left.");
             return;
        }
        State = GameState.InProgress;
        Timer = timeLimit;

        roundStartEvent.Raise();
    }

    void EndRound()
    {
        State = GameState.Ended;
        Debug.Log("[Round] " + CurrentRound + " Ended");
        
        
        if (CurrentRound >= 12)
        {
            Debug.Log("Final Round Ended");
            gameEndEvent.Raise();
        }
        else
        {
            State = GameState.Started;
        }
    }

    public override void Spawned()
    {
        if (!Runner.IsServer) return;
        CurrentRound = 0;
        State = GameState.Waitng;
        Debug.Log("Game Started");

        gameStartEvent.Raise();
    }

}
