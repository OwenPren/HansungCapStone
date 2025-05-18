using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

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

    private bool isWaiting = false;
    private float waitTimer = 0f;

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
                    EndRound();
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

    [Header("Persistent Manger")]
    public StockMarketManager stockMarketManager;
    public UIManager UIManager;


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
    
    // ------------------------------------------------------------

    public class PlayerData 
    {
        public int id;
        public string name;
    }

    public void UpdateStockPrice(JObject Output)
    {
        if (Output.TryGetValue("eventInfo", out JToken eventInfoToken) && eventInfoToken is JArray eventInfoArray)
        {
            int len = eventInfoArray.Count;
            for (int i = 0; i < len; i++)
            {
                JToken eventToken = eventInfoArray[i];
                if (eventToken.Type == JTokenType.Object)
                {
                    JObject eventObject = eventToken.ToObject<JObject>();
                    string impactDirection = eventObject.TryGetValue("impactDirection", out JToken directionToken)
                                             ? directionToken.ToObject<string>()
                                             : null;
                    if (eventObject.TryGetValue("affectedSectors", out JToken sectorsToken) && sectorsToken is JArray affectedSectorsArray)
                    {
                        Debug.Log($"  Processing event {i}: Direction={impactDirection}, Affected Sectors Count={affectedSectorsArray.Count}");
                        foreach (JToken sectorToken in affectedSectorsArray)
                        {
                            if (sectorToken.Type == JTokenType.String)
                            {
                                string sectorName = sectorToken.ToObject<string>();
                                if (stockMarketManager != null)
                                {
                                    Debug.Log($"{sectorName}: ,{impactDirection}");
                                    stockMarketManager.PriceChange(sectorName, impactDirection);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
