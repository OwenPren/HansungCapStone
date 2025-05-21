using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using System.Linq;

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
    [Networked, OnChangedRender(nameof(OnTimerChanged))] public float Timer { get; private set; }
    [Networked] public int CurrentRound { get; private set; }

    [Header("Persistent Manager")]
    public StockMarketManager stockMarketManager;
    public UIManager UIManager;

    public List<string> HintData = new List<string>();
    public Dictionary<string, string> UpdateSectorImpacts = new Dictionary<string, string>();
    public Dictionary<string, string> SectorImpacts = new Dictionary<string, string>();

    private bool isWaiting = false;
    private float waitTimer = 10.0f;
    private bool firstrun = false;

    public override void FixedUpdateNetwork()
    {
        //클라이언트 제외
        if (!Runner.IsServer) return;

        switch (State)
        {
            case GameState.Waitng:
                // 게임 시작 전 대기 상태:
                break;

            case GameState.Started:
                StartRound();
                break;

            case GameState.InProgress:
                Timer -= Runner.DeltaTime;

                if (Timer <= 0f)
                {
                    EndRound(false);
                }
                break;

            case GameState.Ended:
                waitTimer -= Runner.DeltaTime;

                if (waitTimer <= 0f)
                {
                    EndRound(true);
                }
                break;
        }
    }

    public void StartGame()
    {
        //게임 시작 로직 구현 필요
        Debug.Log("StartGame function");
        State = GameState.Started;
    }

    void StartRound()
    {
        UIManager.UpdateCurrentRanking();
        UIManager.UpdateHintUI(HintData);

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

    void EndRound(bool start)
    {
        State = GameState.Ended;
        UpdateStockPrices(UpdateSectorImpacts);
        Debug.Log("[Round] " + CurrentRound + " Ended");


        if (CurrentRound >= 12)
        {
            Debug.Log("Final Round Ended");
            gameEndEvent.Raise();
        }
        else if (start)
        {
            State = GameState.Started;
            waitTimer = 10.0f;
        }
    }
    private void OnTimerChanged()          // 모든 피어의 렌더 단계에서 실행
    {
        TimerChanged?.Invoke(Timer);       // 정적 이벤트로 UI에 알림
    }
    public static event System.Action<float> TimerChanged;

    public override void Spawned()
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


        if (!Runner.IsServer) return;
        CurrentRound = 0;
        State = GameState.Waitng;
        Debug.Log("Game Started");

        gameStartEvent.Raise();
    }

    // ------------------------------------------------------------
    public static GameManager Instance { get; private set; }


    [Header("GameScene Specific")]
    public GameObject playerManagerPrefab; // 게임 씬에서 생성될 PlayerManager 오브젝트
    private Dictionary<PlayerRef, PlayerManager> playerManagers = new Dictionary<PlayerRef, PlayerManager>();

    public void RegisterPlayerManager(PlayerRef playerRef, PlayerManager manager)
    {
        if (!playerManagers.ContainsKey(playerRef))
        {
            playerManagers.Add(playerRef, manager);
            Debug.Log($"[Server] Register PlayerManager {playerRef}");
        }
    }

    public void HandleBuyRequest(PlayerRef sender, string stockName, int quantity)
    {
        if (playerManagers.TryGetValue(sender, out var playerManager))
        {
            // PlayerManager의 BuyStock 로직 실행
            bool success = playerManager.BuyStock(stockName, quantity);
            Debug.Log($"[Server] Buy Request from {sender}: {success}");
            UIManager.UpdateCurrentCashandValue();
        }
        else
        {
            Debug.LogError($"[Server] PlayerManager not found for sender {sender} during Buy Request.");
        }
    }

    public void HandleSellRequest(PlayerRef sender, string stockName, int quantity)
    {
        if (playerManagers.TryGetValue(sender, out var playerManager))
        {
            StockData stock = stockMarketManager.GetStockData(stockName);
            if (stock != null && stock.currentPrice > 0)
            {
                // PlayerManager의 SellStock 로직 실행
                bool success = playerManager.SellStock(stockName, quantity);
                Debug.Log($"[Server] Sell Request from {sender}: {success}");
                UIManager.UpdateCurrentCashandValue();

            }
            else
            {
                Debug.LogError($"[Server] Could not get current price for {stockName} during sell request from {sender}");
            }
        }
        else
        {
            Debug.LogError($"[Server] PlayerManager not found for sender {sender} during Sell Request.");
        }
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public class PlayerData
    {
        public int id;
        public string name;
    }

    public void UpdateStockPrices(Dictionary<string, string> sectorImpacts)
    {
        if (sectorImpacts == null || sectorImpacts.Count == 0)
        {
            Debug.LogWarning("전달된 섹터 영향 딕셔너리가 비어 있습니다. 주가 업데이트를 건너뜀.");
            return;
        }

        // 1. 각 섹터의 주가 변동 적용
        foreach (var entry in sectorImpacts)
        {
            string sectorName = entry.Key;
            string impactDirection = entry.Value;

            if (stockMarketManager != null)
            {
                stockMarketManager.PriceChange(sectorName, impactDirection);
                Debug.Log($"[StockMarket] 섹터 {sectorName} 주가 변동 적용: {impactDirection}");
            }
            else
            {
                Debug.LogError("StockMarketManager가 할당되지 않았습니다. 주가 변동을 적용할 수 없습니다.");
            }
        }

        // 모든 주가 변동이 적용된 후 한 번만 PriceUpdate 호출
        if (stockMarketManager != null)
        {
            stockMarketManager.PriceUpdate();
            Debug.Log("[StockMarket] 모든 주가 변동 업데이트 완료.");
        }

        // 2. 모든 플레이어의 포트폴리오 가치 업데이트
        if (playerManagers == null || playerManagers.Count == 0)
        {
            Debug.LogWarning("PlayerManagers 딕셔너리가 비어 있거나 할당되지 않았습니다. 플레이어 포트폴리오 가치 업데이트를 건너뜜.");
        }
        else
        {
            foreach (var kvp in playerManagers)
            {
                PlayerManager playerManager = kvp.Value;

                if (playerManager != null)
                {
                    // PlayerManager.portfolio가 public이거나 GetPlayerPortfolio()와 같은 메서드로 접근 가능하다고 가정
                    playerManager.ValuationUpdate(playerManager.portfolio);
                    Debug.Log($"플레이어의 포트폴리오 가치 업데이트 완료."); 
                }
                else
                {
                    Debug.LogWarning($"특정 플레이어의 PlayerManager가 null입니다.");
                }
            }
        }

        // 3. UI 업데이트
        //UIManager가 싱글톤이라면
         if (UIManager != null)
        {
            UIManager.UpdateCurrentCashandValue();
        }

        UpdateSectorImpacts = SectorImpacts;
    }

    public void ToGmHintData(List<string> description)
    {
        HintData = description;
    }

    public void ToGmSectorImpacts(Dictionary<string, string> SectorImpacts)
    {
        this.SectorImpacts = SectorImpacts;

        if ( !firstrun )
        {
            UpdateSectorImpacts = SectorImpacts;
            firstrun = !firstrun;
        }
    }

    private bool AreFloatsEqual(float f1, float f2, float tolerance)
    {
        return Mathf.Abs(f1 - f2) <= tolerance;
    }

    // 순위와 함께 PlayerRef 또는 기타 정보를 표시
    public List<(int Rank, PlayerRef PlayerRef, PlayerManager PlayerManager)> GetRankedPlayersWithInfo()
    {
        if (this.playerManagers == null || this.playerManagers.Count == 0)
        {
            Debug.LogWarning("Player managers dictionary is empty or null. Cannot determine ranks.");
            return new List<(int, PlayerRef, PlayerManager)>();
        }

        // 1. 딕셔너리의 각 키-값 쌍을 가져와 PlayerManager의 playerValue를 기준으로 내림차순 정렬합니다.
        //    OrderByDescending 결과는 IOrderedEnumerable 이므로, ToList()로 변환하여 인덱스 접근 가능하게 합니다.
        var sortedPlayerList = playerManagers.OrderByDescending(pair => pair.Value.playerValue).ToList();

        List<(int Rank, PlayerRef PlayerRef, PlayerManager PlayerManager)> rankedList = new List<(int, PlayerRef, PlayerManager)>();

        int currentRank = 1; // 현재 플레이어에게 할당될 순위
        int tie = 1;
        float previousValue = 0f; // 이전 플레이어의 playerValue를 저장 (첫 플레이어와 비교 시 항상 다르게 초기화)
        const float rankTolerance = 0.001f; // 순위 결정에 사용할 오차 범위

        // 2. 정렬된 리스트를 순회하며 순위를 결정하고 결과 리스트를 채웁니다.
        for (int i = 0; i < sortedPlayerList.Count; i++)
        {
            var pair = sortedPlayerList[i]; // 현재 순회 중인 플레이어의 키-값 쌍
            float currentValue = pair.Value.playerValue; // 현재 플레이어의 값

            // 첫 번째 플레이어이거나 (i == 0),
            if (i == 0)
            {
                previousValue = currentValue;
                rankedList.Add((currentRank, pair.Key, pair.Value));
            }
            else if (AreFloatsEqual(currentValue, previousValue, rankTolerance))
            {
                rankedList.Add((currentRank-tie, pair.Key, pair.Value));
                tie++;
            }
            else
            {
                previousValue = currentValue;
                rankedList.Add((currentRank, pair.Key, pair.Value));
                tie = 1;
            }
            currentRank++;
        }
        return rankedList;
    }
}
