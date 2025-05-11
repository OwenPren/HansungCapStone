using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // ------------------------------------------------------------
    public static GameManager Instance { get; private set; }

    [Header("Persistent Manger")]
    public StockMarketManager stockMarketManager;

    [Header("GameScene Specific")]
    public GameObject playerManagerPrefab; // 게임 씬에서 생성될 PlayerManager 오브젝트
    private Dictionary<int, PlayerManager> playerManagers = new Dictionary<int, PlayerManager>();
    public PlayerManager localPlayerManager { get; private set; } // 다른 스크립트에서 접근 가능하도록 public getter 설정

    void Awake() // 싱글톤 패턴
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // 구현 미완료 : 로비/네트워크 시스템으로부터 게임 시작 신호를 받고 게임 씬 로드 및 설정
        // LoadGameScene(); 
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
        SceneManager.sceneLoaded += OnGameSceneLoaded; // 씬 로드 완료 후 호출 이벤트 등록
    }
    
    // ------------------------------------------------------------

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // GameScene이 로드되었는지 확인
        if (scene.name == "GameScene")
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded; // 이벤트 등록했던거 해제

            // 구현 미완료 : 로비/네트워크 시스템으로부터 방에 참여한 플레이어 목록과 로컬 플레이어 ID를 받아서 전달
            List<PlayerData> playersInRoom = GetPlayersFromLobbySystem(); // 가상 함수
            int localPlayerNetworkId = GetLocalPlayerIdFromNetworking(); // 가상 함수

            SetupGameScene();
        }
    }

    private List<PlayerData> GetPlayersFromLobbySystem() {
        /* ... */ 
        return null; 
    }

    private int GetLocalPlayerIdFromNetworking() {
        /* ... */ 
        return 0; 
    }

    // 구현 미완료 : 로비/네트워크 시스템과 연동하여 플레이어 정보를 가져오는 함수 구현 필요

    // 플레이어 오브젝트 PlyaerManager 생성 --> 옮기는걸로
    public void SetupGameScene()
    {
        // 딕셔너리를 초기화를 하고
        // 딕셔너리마다 인벤토리들을 초기화
    }

    public class PlayerData 
    {
        public int id;
        public string name;
    }

}
