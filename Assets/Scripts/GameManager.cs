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
    private PlayerDatabaseSO _db;            // 주입받음

    //Network Object
    //[SerializeField] private NetworkObject playerPrefab;
    [Networked] public GameState State { get; private set; }
    [Networked] public float Timer { get; private set; }
    [Networked] public int CurrentRound { get; private set; }

    private bool isWaiting = false;
    private float waitTimer = 0f;

    public void InitAfterLoad(PlayerDatabaseSO db)
    {
        _db = db;

        if (Object.HasStateAuthority)        // Host 만 실행
            SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        foreach (var p in Runner.ActivePlayers)
        {
            // 1) DB에서 런타임 SO 찾기
            var so = _db.Find(p);

            // 2) 로비에서 Spawn 돼 있던 플레이어 오브젝트 찾기
            if (Runner.TryGetPlayerObject(p, out var obj))
            {
                // 3) PlayerNetwork 컴포넌트에 SO 주입
                var pn = obj.GetComponent<PlayerNetwork>();
                pn.runtimeData = so;
            }
            else
            {
                // (선택) 로비에서 Spawn 하지 않았다면:
                // var obj = Runner.Spawn(playerPrefab, GetSpawnPos(p), Quaternion.identity, p);
                // obj.GetComponent<PlayerNetwork>().runtimeData = so;
            }
        }
    }
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

    void GenerateSituation()
    {
        // 여기를 ChatAssistant 연결하여 상황 부여 받을 예정
        // currentSituation = 한 대형 IT 회사가 신형 소프트웨어 기술을 발표했다.";
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

        roundStartEvent.Raise(this);
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

    void EvaluateRound()
    {
        // 여기에서 수익 반영, 순위 계산 등 수행
        Debug.Log("라운드 결과 평가 시작");
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
