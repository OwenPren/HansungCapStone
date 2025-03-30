using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum GameState
{
    Wating, // 게임 시작 대기중
    InProgress, // 라운드 진행중
    Started, // 라운드 시작
    Ended, // 라운드 종료

}


public class GameManager : NetworkBehaviour
{
    public float timeLimit = 60f;
    
    //Sriptable Object
    public GameStartEventSO gameStartEvent;

    //Network Object
    [Networked] public GameState State { get; private set; }
    [Networked] public float Timer { get; private set; }

    public override void FixedUpdateNetwork()
    {
        //클라이언트 제외
        if (!Runner.IsServer) return;

        switch (State)
        {
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

    void StartRound()
    {
        State = GameState.InProgress;
        Timer = timeLimit;
        Debug.Log("Round Started");
    }

    void EndRound()
    {
        State = GameState.Ended;
        Debug.Log("Round Ended");
    }


    public override void Spawned()
    {
        if (!Runner.IsServer) return;

        State = GameState.Wating;
        Debug.Log("Game Started");

        Debug.Log("Thread Raise");
        gameStartEvent.Raise();
    }

}
