using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using System.Linq;
using Photon.Realtime;

public enum GameState
{
    Waitng, // 게임 시작 대기중
    InProgress, // 라운드 진행중
    Started, // 라운드 시작
    Ended, // 라운드 종료
    Finished, // 게임 종료
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
    [Networked, OnChangedRender(nameof(OnTimerChanged))] public float waitTimer { get; private set; }
    [Networked] public int CurrentRound { get; private set; }

    [Header("Persistent Manager")]
    public StockMarketManager stockMarketManager;
    public UIManager UIManager;

    [Networked, Capacity(10)] public NetworkArray<NetworkString<_256>> NetworkedHintData { get; }
    public List<string> HintData = new List<string>();
    public Dictionary<string, string> UpdateSectorImpacts = new Dictionary<string, string>();
    public Dictionary<string, string> SectorImpacts = new Dictionary<string, string>();

    private bool isWaiting = false;
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

            case GameState.Finished:
                waitTimer -= Runner.DeltaTime;

                if (waitTimer <= 0f && Runner.IsServer)
                {
                    _ = Runner.Shutdown();   // 세션 전체를 깨끗하게 종료
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
        if (CurrentRound == 0)
        {
            // 모든 클라이언트에 랭킹 업데이트 알림
            RpcUpdateCurrentRanking();

            // 추가: 첫 라운드 시작 시 주가 데이터 재동기화
            if (Runner.IsServer)
            {
                StartCoroutine(EnsureStockDataSync());
            }
        }

        // 모든 클라이언트에 UI 업데이트 알림
        Rpc_UpdatePlayerUI();
        RpcShowGamePanel();
        RpcShowResultPanel(false);
        RpcUpdateHintUI();

        CurrentRound++;
        Debug.Log("[Round] " + CurrentRound + " Started");
        if (CurrentRound > 12)
        {
            Debug.Log("No more rounds left.");
            return;
        }
        State = GameState.InProgress;
        Timer = timeLimit;
        waitTimer = 10.0f;

        roundStartEvent.Raise(this);
    }


    void EndRound(bool start)
    {
        Debug.Log($"[GameManager] EndRound called - start: {start}, IsServer: {Runner.IsServer}");

        State = GameState.Ended;

        if (!start)
        {
            Debug.Log("[GameManager] Processing round end (not start)");

            // 주가 업데이트 (서버에서만 실행되고 RPC로 클라이언트에 전송)
            Debug.Log($"[GameManager] About to call UpdateStockPrices. UpdateSectorImpacts count: {UpdateSectorImpacts?.Count ?? 0}");

            if (UpdateSectorImpacts != null && UpdateSectorImpacts.Count > 0)
            {
                foreach (var kvp in UpdateSectorImpacts)
                {
                    Debug.Log($"[GameManager] SectorImpact: {kvp.Key} = {kvp.Value}");
                }
            }

            UpdateStockPrices(UpdateSectorImpacts);

            // 모든 클라이언트에 랭킹 업데이트 알림
            RpcUpdateCurrentRanking();

            // 주가 데이터 전송 후 UI 업데이트
            if (Runner.IsServer)
            {
                Debug.Log("[GameManager] Starting DelayedCompleteUIUpdate");
                StartCoroutine(DelayedCompleteUIUpdate());
            }
        }

        // 모든 클라이언트에 결과 UI 업데이트 알림
        RpcUpdateResultUI();
        RpcShowResultPanel(true);

        Debug.Log("[Round] " + CurrentRound + " Ended");

        if (CurrentRound >= 12)
        {
            Debug.Log("Final Round Ended");
            gameEndEvent.Raise();
            waitTimer = 20.0f;
            State = GameState.Finished;
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
        TimerChanged?.Invoke(waitTimer);
    }
    public static event System.Action<float> TimerChanged;

    // 게임 시작 시 클라이언트 동기화 체크 시작
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

        if (Runner.IsServer)
        {
            CurrentRound = 0;
            State = GameState.Waitng;
            Debug.Log("[GameManager] (SERVER) Game Started");
            gameStartEvent.Raise();

            // 추가: 서버에서 초기 주가 동기화 시작
            StartCoroutine(ServerInitialStockSync());
        }
        else
        {
            Debug.Log("[GameManager] (CLIENT) Game Started - starting sync check");
            StartClientSyncCheck();

            // 추가: 클라이언트에서 초기 주가 데이터 요청
            StartCoroutine(ClientInitialStockSync());
        }
    }


    // ------------------------------------------------------------
    public static GameManager Instance { get; private set; }

    [Header("GameScene Specific")]
    private Dictionary<PlayerRef, PlayerManager> playerManagers = new Dictionary<PlayerRef, PlayerManager>();

    // ============= RPC 메서드들 =============

    // 클라이언트에서 서버로 구매 요청을 보내는 RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcBuyStockRequest(PlayerRef requestingPlayer, string stockName, int quantity)
    {
        Debug.Log($"[GameManager] RpcBuyStockRequest received - Player: {requestingPlayer}, Stock: {stockName}, Quantity: {quantity}");

        // 매개변수로 받은 PlayerRef 사용
        HandleBuyRequest(requestingPlayer, stockName, quantity);
    }

    // 클라이언트에서 서버로 판매 요청을 보내는 RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcSellStockRequest(PlayerRef requestingPlayer, string stockName, int quantity)
    {
        Debug.Log($"[GameManager] RpcSellStockRequest received - Player: {requestingPlayer}, Stock: {stockName}, Quantity: {quantity}");

        // 매개변수로 받은 PlayerRef 사용
        HandleSellRequest(requestingPlayer, stockName, quantity);
    }

    // ============= UI 동기화 RPC들 =============

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdateCurrentRanking()
    {
        Debug.Log("[GameManager] RpcUpdateCurrentRanking HandleBuyRequestreceived");

        // 각 클라이언트의 UIManager 찾기
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateCurrentRanking();
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager not found in RpcUpdateCurrentRanking");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdateResultUI()
    {
        Debug.Log("[GameManager] RpcUpdateResultUI received");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateResultUI();
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager not found in RpcUpdateResultUI");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcShowResultPanel(bool show)
    {
        Debug.Log($"[GameManager] RpcShowResultPanel received - show: {show}");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowResultPanel(show);
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager not found in RpcShowResultPanel");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcShowGamePanel()
    {
        Debug.Log("[GameManager] RpcShowGamePanel received");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowGamePanel();
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager not found in RpcShowGamePanel");
        }
    }

    // 힌트 UI 업데이트 RPC (힌트 데이터를 매개변수로 전송)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdateHintUI()
    {
        Debug.Log($"[GameManager] RpcUpdateHintUI called - HintData count: {HintData.Count}");

        // 최대 2개까지만 전송
        string hint1 = HintData.Count > 0 ? HintData[0] : "";
        string hint2 = HintData.Count > 1 ? HintData[1] : "";

        RpcUpdateHintUIWithData(hint1, hint2);
    }


    // 주식 거래 후 UI 업데이트
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdatePlayerUI()
    {
        Debug.Log("[GameManager] RpcUpdatePlayerUI received");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateCurrentCashandValue();
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager not found in RpcUpdatePlayerUI");
        }
    }

    private System.Collections.IEnumerator DelayedMarketUIUpdate()
    {
        yield return new WaitForSeconds(0.2f);

        if (Runner.IsServer)
        {
            RpcForceMarketUIUpdate();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcForceMarketUIUpdate()
    {
        Debug.Log("[GameManager] RpcForceMarketUIUpdate received");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateMarketStockUI();
            Debug.Log("[GameManager] Market UI force updated");
        }
    }

    public void PmRoundStartCall()
    {
        foreach (var kvp in playerManagers)
            kvp.Value.SetPreviousValue();
    }

    public void UnregisterPlayerManager(PlayerRef player)
    {
        if (player == default(PlayerRef))
        {
            Debug.LogWarning($"[GameManager] Attempting to unregister PlayerManager with invalid PlayerRef (None/default)");
            return;
        }

        if (playerManagers.ContainsKey(player))
        {
            Debug.Log($"[GameManager] Unregistering PlayerManager for player {player}");
            playerManagers.Remove(player);
            Debug.Log($"[GameManager] Remaining players: {playerManagers.Count}");
        }
        else
        {
            Debug.LogWarning($"[GameManager] Attempted to unregister non-existent player {player}");
        }
    }
    public void RegisterPlayerManager(PlayerRef playerRef, PlayerManager manager)
    {
        if (playerRef == default(PlayerRef))
        {
            Debug.LogError($"[GameManager] Attempting to register PlayerManager with invalid PlayerRef (None/default)");
            return;
        }

        if (manager == null)
        {
            Debug.LogError($"[GameManager] Attempting to register null PlayerManager for player {playerRef}");
            return;
        }

        string context = Runner?.IsServer == true ? "SERVER" : "CLIENT";

        if (!manager.IsSpawned)
        {
            Debug.LogWarning($"[GameManager] ({context}) Attempting to register PlayerManager that is not spawned for player {playerRef}");
        }

        if (!playerManagers.ContainsKey(playerRef))
        {
            playerManagers.Add(playerRef, manager);
            Debug.Log($"[GameManager] ({context}) Successfully registered PlayerManager for player {playerRef}. Total players: {playerManagers.Count}");

            // 클라이언트에서 등록 시 동기화 상태 확인
            if (Runner?.IsServer != true)
            {
                VerifyClientSync(playerRef, manager);
            }
        }
        else
        {
            Debug.LogWarning($"[GameManager] ({context}) PlayerManager for {playerRef} already exists, updating...");
            playerManagers[playerRef] = manager;
        }
    }
    private void VerifyClientSync(PlayerRef playerRef, PlayerManager manager)
    {
        Debug.Log($"[GameManager] (CLIENT) Verifying sync for player {playerRef}");

        // PlayerInfoManager와 동기화 확인
        if (PlayerInfoManager.Instance != null)
        {
            var playerInfo = PlayerInfoManager.Instance.GetPlayerInfo(playerRef);
            if (playerInfo.HasValue)
            {
                Debug.Log($"[GameManager] (CLIENT) PlayerInfo found for {playerRef}: '{playerInfo.Value.nickname.ToString()}'");

                // PlayerManager에 정보 업데이트 - 권한 체크 제거하고 로컬 필드만 업데이트
                manager.NameField = playerInfo.Value.nickname.ToString();
                Debug.Log($"[GameManager] Updated NameField for {playerRef}");
            }
            else
            {
                Debug.LogWarning($"[GameManager] (CLIENT) No PlayerInfo found for {playerRef}");
            }
        }

        // 클라이언트에서는 RPC 호출하지 않고 로컬 UI만 업데이트
        StartCoroutine(DelayedLocalUIUpdate());
    }

    private System.Collections.IEnumerator DelayedLocalUIUpdate()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("[GameManager] (CLIENT) Updating local UI after PlayerManager registration");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateCurrentRanking();
            uiManager.UpdateCurrentCashandValue();
            uiManager.UpdateMarketStockUI();
        }
    }

    private System.Collections.IEnumerator DelayedUIUpdate()
    {
        yield return new WaitForSeconds(1f);

        if (Runner?.IsServer != true) // 클라이언트에서만
        {
            Debug.Log("[GameManager] (CLIENT) Requesting UI update after PlayerManager registration");
            RpcUpdateCurrentRanking();
        }
    }

    public PlayerManager GetPlayerManager(PlayerRef playerRef)
    {
        if (playerManagers.TryGetValue(playerRef, out PlayerManager manager))
        {
            // 유효성 체크
            if (manager != null && manager.IsSpawned)
            {
                return manager;
            }
            else
            {
                Debug.LogWarning($"[GameManager] PlayerManager for {playerRef} exists but is not valid");
                return null;
            }
        }

        Debug.LogWarning($"[GameManager] PlayerManager not found for {playerRef}");
        return null;
    }

    public void HandleBuyRequest(PlayerRef sender, string stockName, int quantity)
    {
        Debug.Log($"[GameManager] HandleBuyRequest - Player: {sender}, Stock: {stockName}, Quantity: {quantity}");

        if (playerManagers.TryGetValue(sender, out var playerManager))
        {
            if (playerManager == null || !playerManager.IsSpawned)
            {
                Debug.LogError($"[GameManager] PlayerManager for {sender} is null or not spawned");
                return;
            }

            bool success = playerManager.BuyStock(stockName, quantity);
            Debug.Log($"[GameManager] Buy Request from {sender}: {(success ? "SUCCESS" : "FAILED")}");

            // 성공한 경우에만 UI 업데이트 (약간의 지연 후)
            if (success)
            {
                StartCoroutine(DelayedUIUpdate(sender, 0.1f));
            }
        }
        else
        {
            Debug.LogError($"[GameManager] PlayerManager not found for sender {sender} during Buy Request.");
        }
    }

    public void HandleSellRequest(PlayerRef sender, string stockName, int quantity)
    {
        Debug.Log($"[GameManager] HandleSellRequest - Player: {sender}, Stock: {stockName}, Quantity: {quantity}");

        if (playerManagers.TryGetValue(sender, out var playerManager))
        {
            if (playerManager == null || !playerManager.IsSpawned)
            {
                Debug.LogError($"[GameManager] PlayerManager for {sender} is null or not spawned");
                return;
            }

            StockData stock = stockMarketManager?.GetStockData(stockName);
            if (stock != null && stock.currentPrice > 0)
            {
                bool success = playerManager.SellStock(stockName, quantity);
                Debug.Log($"[GameManager] Sell Request from {sender}: {(success ? "SUCCESS" : "FAILED")}");

                // 성공한 경우에만 UI 업데이트 (약간의 지연 후)
                if (success)
                {
                    StartCoroutine(DelayedUIUpdate(sender, 0.1f));
                }
            }
            else
            {
                Debug.LogError($"[GameManager] Could not get current price for {stockName} during sell request from {sender}");
            }
        }
        else
        {
            Debug.LogError($"[GameManager] PlayerManager not found for sender {sender} during Sell Request.");
        }
    }

    // 지연된 UI 업데이트를 위한 코루틴
    private System.Collections.IEnumerator DelayedUIUpdate(PlayerRef targetPlayer, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 서버에서만 RPC 호출
        if (Runner.IsServer)
        {
            // 특정 플레이어에게만 UI 업데이트 RPC 전송
            RpcUpdatePlayerUIForTarget(targetPlayer);

            // 모든 플레이어에게 랭킹 업데이트
            RpcUpdateCurrentRanking();
        }
        else
        {
            // 클라이언트에서는 로컬 UI만 업데이트
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateCurrentCashandValue();
                uiManager.UpdateMarketStockUI();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdatePlayerUIForTarget(PlayerRef targetPlayer)
    {
        Debug.Log($"[GameManager] RpcUpdatePlayerUIForTarget received - Target: {targetPlayer}");

        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && runner.LocalPlayer == targetPlayer)
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateCurrentCashandValue(); // 현금/가치 업데이트
                uiManager.UpdateMarketStockUI(); // 시장 UI 업데이트 (보유 주식 수량 포함)
                Debug.Log($"[GameManager] {targetPlayer} UI Update Success");
            }
            else
            {
                Debug.LogError($"[GameManager] UIManager not found for {targetPlayer}");
            }
        }
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_UpdatePlayerUI(PlayerRef targetPlayer)
    {
        // 이 RPC는 targetPlayer에 해당하는 클라이언트에서만 실행됩니다.
        if (Runner.LocalPlayer == targetPlayer)
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateCurrentCashandValue(); // 현금/가치 업데이트
                uiManager.UpdateMarketStockUI(); // 시장 UI 업데이트 (보유 주식 수량 포함)
                Debug.Log($"[GameManager] {targetPlayer} UI Update Success");
            }
            else
            {
                Debug.LogError($"[GameManager] Client {targetPlayer} no found for UI Update");
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_UpdatePlayerUI()
    {
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateCurrentCashandValue(); // 현금/가치 업데이트
            uiManager.UpdateMarketStockUI(); // 시장 UI 업데이트 (보유 주식 수량 포함)
            Debug.Log($"[GameManager] ALL player Market UI Update Success");
        }
        else
        {
            Debug.LogError($"[GameManager] Client ALL Player Market no found for UI Update");
        }

    }

    // 클라이언트용 강제 동기화 메서드
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestFullSync(PlayerRef requestingPlayer)
    {
        Debug.Log($"[GameManager] RpcRequestFullSync received from {requestingPlayer}");

        if (!Runner.IsServer) return;

        // 서버에서 해당 클라이언트에게 전체 상태 동기화
        RpcSendFullSyncToClient(requestingPlayer);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSendFullSyncToClient(PlayerRef targetPlayer)
    {
        Debug.Log($"[GameManager] RpcSendFullSyncToClient - Target: {targetPlayer}");

        // 모든 UI 요소 업데이트
        RpcUpdateCurrentRanking();
        RpcShowGamePanel();
        RpcShowResultPanel(false);
        RpcUpdateHintUI();

        Debug.Log("[GameManager] Full sync sent to clients");
    }

    // 클라이언트에서 주기적으로 동기화 상태 확인
    public void StartClientSyncCheck()
    {
        if (Runner?.IsServer != true) // 클라이언트에서만
        {
            StartCoroutine(ClientSyncCheckLoop());
        }
    }

    private System.Collections.IEnumerator ClientSyncCheckLoop()
    {
        yield return new WaitForSeconds(5f); // 초기 지연

        while (State == GameState.InProgress || State == GameState.Waitng)
        {
            CheckClientSync();
            yield return new WaitForSeconds(10f); // 10초마다 체크
        }
    }

    private void CheckClientSync()
    {
        if (Runner?.IsServer == true) return; // 서버에서는 실행하지 않음

        int playerInfoCount = PlayerInfoManager.Instance?.PlayerInfos.Count ?? 0;
        int playerManagerCount = playerManagers.Count;

        Debug.Log($"[GameManager] (CLIENT) Sync check - PlayerInfo: {playerInfoCount}, PlayerManager: {playerManagerCount}");

        if (playerInfoCount > 0 && playerManagerCount == 0)
        {
            Debug.LogWarning("[GameManager] (CLIENT) Sync mismatch detected! Requesting full sync...");
            RpcRequestFullSync(Runner.LocalPlayer);
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
        Debug.Log($"[GameManager] UpdateStockPrices called. IsServer: {Runner.IsServer}, SectorImpacts count: {sectorImpacts?.Count ?? 0}");

        if (sectorImpacts == null || sectorImpacts.Count == 0)
        {
            Debug.LogWarning("전달된 섹터 영향 딕셔너리가 비어 있습니다. 주가 업데이트를 건너뜀.");
            return;
        }

        // 서버에서만 실행되는 부분
        if (Runner.IsServer)
        {
            Debug.Log("[GameManager] (SERVER) Starting stock price update process");

            // 1. 먼저 모든 주식의 현재가를 이전가로 설정 (변동률 계산을 위해)
            if (stockMarketManager != null)
            {
                foreach (var stock in stockMarketManager.allStocks)
                {
                    stock.previousPrice = stock.currentPrice;
                    Debug.Log($"[GameManager] Set previous price for {stock.stockName}: {stock.previousPrice:F1}");
                }
            }

            // 2. 각 섹터의 주가 변동 적용
            foreach (var entry in sectorImpacts)
            {
                string sectorName = entry.Key;
                string impactDirection = entry.Value;

                Debug.Log($"[GameManager] Applying price change - Sector: {sectorName}, Direction: {impactDirection}");

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

            // 3. 서버에서 변동률 계산
            if (stockMarketManager != null)
            {
                stockMarketManager.PriceUpdate();
                Debug.Log("[StockMarket] 서버에서 모든 주가 변동 업데이트 완료.");

                // 4. 변동된 주가 정보를 로그로 확인
                foreach (var stock in stockMarketManager.allStocks)
                {
                    Debug.Log($"[GameManager] Stock after update - {stock.stockName}: Current={stock.currentPrice:F1}, Previous={stock.previousPrice:F1}, Change={stock.stockChangeRate:F2}%");
                }

                // 5. 클라이언트들에게 업데이트된 주가 정보 전송
                Debug.Log("[GameManager] Sending stock data to all clients...");
                foreach (var stock in stockMarketManager.allStocks)
                {
                    Debug.Log($"[GameManager] Sending RPC for {stock.stockName}: {stock.currentPrice:F1}, {stock.previousPrice:F1}, {stock.stockChangeRate:F2}%");
                    RpcUpdateStockData(stock.stockName, stock.currentPrice, stock.previousPrice, stock.stockChangeRate);
                }
            }
        }
        else
        {
            Debug.Log("[GameManager] (CLIENT) UpdateStockPrices called but not server - skipping");
        }

        // 6. 모든 플레이어의 포트폴리오 가치 업데이트 (서버에서만)
        if (Runner.IsServer && playerManagers != null && playerManagers.Count > 0)
        {
            Debug.Log("[GameManager] Updating player portfolio values...");
            foreach (var kvp in playerManagers)
            {
                PlayerManager playerManager = kvp.Value;

                if (playerManager != null && playerManager.IsSpawned)
                {
                    playerManager.ValuationUpdate(playerManager.portfolio);
                    Debug.Log($"플레이어 {kvp.Key}의 포트폴리오 가치 업데이트 완료.");
                }
                else
                {
                    Debug.LogWarning($"플레이어 {kvp.Key}의 PlayerManager가 null이거나 spawned되지 않았습니다.");
                }
            }
        }

        // 7. UI 업데이트
        if (UIManager != null)
        {
            UIManager.UpdateCurrentCashandValue();
        }

        UpdateSectorImpacts = SectorImpacts;
        Debug.Log("[GameManager] UpdateStockPrices completed");
    }

    public void ToGmHintData(List<string> description)
    {
        // 최대 2개 힌트까지만 저장
        HintData = new List<string>();
        for (int i = 0; i < description.Count && i < 2; i++)
        {
            HintData.Add(description[i]);
        }

        Debug.Log($"[GameManager] ToGmHintData called with {description.Count} hints, storing {HintData.Count} hints");

        if (Runner != null && Runner.IsServer)
        {
            // 힌트 데이터를 RPC로 전송
            string hint1 = HintData.Count > 0 ? HintData[0] : "";
            string hint2 = HintData.Count > 1 ? HintData[1] : "";

            Debug.Log($"[GameManager] Sending hints to clients - Hint1: '{hint1}', Hint2: '{hint2}'");

            RpcUpdateHintUIWithData(hint1, hint2);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdateStockData(string stockName, float currentPrice, float previousPrice, float changeRate)
    {
        Debug.Log($"[GameManager] RpcUpdateStockData received - {stockName}: Current={currentPrice:F1}, Previous={previousPrice:F1}, Change={changeRate:F2}%. IsServer: {Runner.IsServer}");

        if (stockMarketManager != null)
        {
            StockData stock = stockMarketManager.GetStockData(stockName);
            if (stock != null)
            {
                float oldCurrentPrice = stock.currentPrice;
                float oldChangeRate = stock.stockChangeRate;

                stock.currentPrice = currentPrice;
                stock.previousPrice = previousPrice;
                stock.stockChangeRate = changeRate;

                Debug.Log($"[GameManager] Updated client stock data for {stockName}: {oldCurrentPrice:F1}→{currentPrice:F1}, {oldChangeRate:F2}%→{changeRate:F2}%");
            }
            else
            {
                Debug.LogError($"[GameManager] Stock not found: {stockName}");
            }
        }
        else
        {
            Debug.LogError("[GameManager] StockMarketManager is null in RpcUpdateStockData");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcUpdateHintUIWithData(string hint1 = "", string hint2 = "")
    {
        var hints = new List<string>();

        if (!string.IsNullOrEmpty(hint1)) hints.Add(hint1);
        if (!string.IsNullOrEmpty(hint2)) hints.Add(hint2);

        Debug.Log($"[GameManager] RpcUpdateHintUIWithData received - hint1: '{hint1}', hint2: '{hint2}'");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateHintUI(hints);
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager not found in RpcUpdateHintUIWithData");
        }
    }

    public void ToGmSectorImpacts(Dictionary<string, string> SectorImpacts)
    {
        this.SectorImpacts = SectorImpacts;

        if (!firstrun)
        {
            UpdateSectorImpacts = SectorImpacts;
            firstrun = !firstrun;
        }
    }

    private bool AreFloatsEqual(float f1, float f2, float tolerance)
    {
        return Mathf.Abs(f1 - f2) <= tolerance;
    }

    // 안전한 순위 조회 메서드
    public List<(PlayerRef playerRef, PlayerManager manager, NetworkPlayerInfo info)> GetRankedPlayersWithInfo()
    {
        var playersWithInfo = new List<(PlayerRef playerRef, PlayerManager manager, NetworkPlayerInfo info)>();

        // PlayerInfoManager가 null인지 확인
        if (PlayerInfoManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] PlayerInfoManager.Instance is null in GetRankedPlayersWithInfo");
            return playersWithInfo;
        }

        // playerManagers의 복사본을 만들어 iteration 중 수정으로 인한 문제 방지
        var playerManagersCopy = new Dictionary<PlayerRef, PlayerManager>(playerManagers);

        Debug.Log($"[GameManager] GetRankedPlayersWithInfo - playerManagers count: {playerManagersCopy.Count}");

        foreach (var pair in playerManagersCopy)
        {
            PlayerRef playerRef = pair.Key;
            PlayerManager manager = pair.Value;

            Debug.Log($"[GameManager] Processing player {playerRef}");

            // PlayerRef가 유효한지 확인
            if (playerRef == default(PlayerRef))
            {
                Debug.LogWarning($"[GameManager] Invalid PlayerRef (default/None) detected, skipping");
                continue;
            }

            // PlayerManager가 null이거나 Spawned 상태가 아닌 경우 스킵
            if (manager == null || !manager.IsSpawned)
            {
                Debug.LogWarning($"[GameManager] PlayerManager for {playerRef} is null or not spawned, removing from list");
                // 비동기적으로 제거 (현재 iteration에 영향 주지 않음)
                playerManagers.Remove(playerRef);
                continue;
            }

            // PlayerInfoManager에서 플레이어 정보 가져오기
            var playerInfo = PlayerInfoManager.Instance.GetPlayerInfo(playerRef);
            if (playerInfo.HasValue)
            {
                playersWithInfo.Add((playerRef, manager, playerInfo.Value));
                Debug.Log($"[GameManager] Successfully added player {playerRef} with nickname '{playerInfo.Value.nickname.ToString()}'");
            }
            else
            {
                Debug.LogWarning($"[GameManager] No player info found for {playerRef}");

                // PlayerInfo가 없는 경우 기본값으로 추가 (임시 해결책)
                NetworkPlayerInfo defaultInfo = new NetworkPlayerInfo("Unknown", manager.NameField ?? "Player", 0);
                playersWithInfo.Add((playerRef, manager, defaultInfo));
                Debug.Log($"[GameManager] Added player {playerRef} with default info");
            }
        }

        Debug.Log($"[GameManager] GetRankedPlayersWithInfo completed - total players: {playersWithInfo.Count}");

        // playerValue 기준으로 정렬 (안전한 접근 메서드 사용)
        return playersWithInfo
            .Where(tuple => tuple.manager != null && tuple.manager.IsSpawned) // 추가 안전 체크
            .OrderByDescending(tuple => tuple.manager.GetPlayerValue())
            .ToList();
    }

    public List<(int rank, PlayerRef playerRef, PlayerManager manager)> GetRankedPlayers()
    {
        // 1) 기존 정렬 로직 재사용
        var ranked = GetRankedPlayersWithInfo()
                    .Select((t, idx) => (rank: idx + 1,
                                        playerRef: t.playerRef,
                                        manager: t.manager))
                    .ToList();
        return ranked;
    }

    public void DebugPlayerManagerState()
    {
        Debug.Log($"[GameManager] === PLAYER MANAGER DEBUG ===");
        Debug.Log($"[GameManager] Total playerManagers: {playerManagers.Count}");

        foreach (var kvp in playerManagers)
        {
            PlayerRef playerRef = kvp.Key;
            PlayerManager manager = kvp.Value;

            Debug.Log($"[GameManager] Player {playerRef}: Manager={manager != null}, IsSpawned={manager?.IsSpawned}, Value={manager?.GetPlayerValue()}");

            if (PlayerInfoManager.Instance != null)
            {
                var playerInfo = PlayerInfoManager.Instance.GetPlayerInfo(playerRef);
                if (playerInfo.HasValue)
                {
                    Debug.Log($"[GameManager]   PlayerInfo: '{playerInfo.Value.nickname.ToString()}'");
                }
                else
                {
                    Debug.Log($"[GameManager]   PlayerInfo: NOT FOUND");
                }
            }
        }

        if (PlayerInfoManager.Instance != null)
        {
            Debug.Log($"[GameManager] PlayerInfoManager PlayerInfos count: {PlayerInfoManager.Instance.PlayerInfos.Count}");
        }
        else
        {
            Debug.Log($"[GameManager] PlayerInfoManager.Instance is NULL");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncStockPrices()
    {
        Debug.Log("[GameManager] RpcSyncStockPrices received");

        if (stockMarketManager != null)
        {
            // 클라이언트에서 주가 변동률 재계산
            stockMarketManager.ClientPriceUpdate();

            // UI 업데이트
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateMarketStockUI();
                Debug.Log("[GameManager] Market UI updated after stock price sync");
            }
        }
        else
        {
            Debug.LogError("[GameManager] StockMarketManager not found in RpcSyncStockPrices");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncStockPreviousPrices(string stockName, float previousPrice)
    {
        Debug.Log($"[GameManager] RpcSyncStockPreviousPrices received - {stockName}: {previousPrice}");

        if (stockMarketManager != null)
        {
            StockData stock = stockMarketManager.GetStockData(stockName);
            if (stock != null)
            {
                stock.previousPrice = previousPrice;
                Debug.Log($"[GameManager] Updated {stockName} previous price to {previousPrice}");
            }
        }
    }

    private System.Collections.IEnumerator DelayedStockSync()
    {
        yield return new WaitForSeconds(0.1f); // 네트워크 전송 시간 대기
        RpcSyncStockPrices();
    }

    private System.Collections.IEnumerator InitialStockSync()
    {
        yield return new WaitForSeconds(3f); // 모든 시스템이 준비될 때까지 대기

        if (stockMarketManager != null)
        {
            Debug.Log("[GameManager] Sending initial stock sync to clients");
            RpcSyncStockPrices();
        }
    }

    // 클라이언트의 주가 동기화 요청
    private System.Collections.IEnumerator ClientStockSyncRequest()
    {
        yield return new WaitForSeconds(5f); // 초기 지연

        while (State == GameState.InProgress || State == GameState.Waitng)
        {
            // 주가 변동률이 모두 0인 경우 동기화 요청
            if (stockMarketManager != null && NeedsStockSync())
            {
                Debug.Log("[GameManager] (CLIENT) Requesting stock price sync");
                RpcRequestStockSync();
            }

            yield return new WaitForSeconds(10f); // 10초마다 체크
        }
    }
    // 주가 동기화가 필요한지 확인
    private bool NeedsStockSync()
    {
        if (stockMarketManager == null || stockMarketManager.allStocks == null) return false;

        int zeroChangeCount = 0;
        foreach (var stock in stockMarketManager.allStocks)
        {
            if (Mathf.Abs(stock.stockChangeRate) < 0.001f)
            {
                zeroChangeCount++;
            }
        }

        // 대부분의 주식이 0% 변동률을 보이면 동기화 필요
        return zeroChangeCount >= stockMarketManager.allStocks.Count - 2;
    }

    // 클라이언트가 서버에 주가 동기화를 요청하는 RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestStockSync()
    {
        Debug.Log("[GameManager] Stock sync requested by client");

        if (Runner.IsServer)
        {
            RpcSyncStockPrices();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncAllInitialStockData()
    {
        Debug.Log("[GameManager] RpcSyncAllInitialStockData received");

        if (!Runner.IsServer && stockMarketManager != null)
        {
            Debug.Log("[GameManager] Client requesting initial stock data from server");
            RpcRequestInitialStockData();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestInitialStockData()
    {
        Debug.Log("[GameManager] Initial stock data requested by client");

        if (Runner.IsServer && stockMarketManager != null)
        {
            var allStockData = stockMarketManager.GetAllStockData();

            foreach (var (name, currentPrice, previousPrice, changeRate) in allStockData)
            {
                RpcSendInitialStockData(name, currentPrice, previousPrice, changeRate);
            }

            Debug.Log($"[GameManager] Sent initial data for {allStockData.Count} stocks to clients");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSendInitialStockData(string stockName, float currentPrice, float previousPrice, float changeRate)
    {
        Debug.Log($"[GameManager] RpcSendInitialStockData received - {stockName}: {currentPrice:F1}원");

        if (!Runner.IsServer && stockMarketManager != null)
        {
            stockMarketManager.SetStockData(stockName, currentPrice, previousPrice, changeRate);

            // UI 업데이트도 함께
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateMarketStockUI();
            }
        }
    }

    private System.Collections.IEnumerator ServerInitialStockSync()
    {
        yield return new WaitForSeconds(2f); // StockMarketManager 초기화 대기

        if (stockMarketManager != null && stockMarketManager.allStocks.Count > 0)
        {
            Debug.Log("[GameManager] (SERVER) Broadcasting initial stock data to all clients");
            RpcSyncAllInitialStockData();
        }
        else
        {
            Debug.LogError("[GameManager] (SERVER) StockMarketManager not ready for initial sync");
        }
    }

    private System.Collections.IEnumerator ClientInitialStockSync()
    {
        yield return new WaitForSeconds(3f); // 서버 준비 대기

        Debug.Log("[GameManager] (CLIENT) Requesting initial stock data");

        // 여러 번 시도 (네트워크 지연 대비)
        for (int i = 0; i < 3; i++)
        {
            if (stockMarketManager != null && stockMarketManager.allStocks.Count > 0)
            {
                bool needsSync = false;
                foreach (var stock in stockMarketManager.allStocks)
                {
                    if (stock.currentPrice < 1000) // 초기화되지 않은 상태
                    {
                        needsSync = true;
                        break;
                    }
                }

                if (needsSync)
                {
                    Debug.Log($"[GameManager] (CLIENT) Attempt {i + 1}: Requesting stock data sync");
                    RpcRequestInitialStockData();
                    yield return new WaitForSeconds(2f);
                }
                else
                {
                    Debug.Log("[GameManager] (CLIENT) Stock data already synced");
                    break;
                }
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private System.Collections.IEnumerator EnsureStockDataSync()
    {
        yield return new WaitForSeconds(0.5f);

        if (stockMarketManager != null)
        {
            Debug.Log("[GameManager] Ensuring stock data sync before first round");

            var allStockData = stockMarketManager.GetAllStockData();
            foreach (var (name, currentPrice, previousPrice, changeRate) in allStockData)
            {
                RpcSendInitialStockData(name, currentPrice, previousPrice, changeRate);
            }

            // UI 업데이트 강제 실행
            yield return new WaitForSeconds(0.3f);
            RpcForceCompleteUIUpdate();
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcForceCompleteUIUpdate()
    {
        Debug.Log("[GameManager] RpcForceCompleteUIUpdate received");

        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateMarketStockUI(); // 주가 정보 업데이트
            uiManager.UpdateCurrentCashandValue(); // 현금/가치 업데이트
            Debug.Log("[GameManager] Complete UI force updated");
        }
        else
        {
            Debug.LogWarning("[GameManager] UIManager not found in RpcForceCompleteUIUpdate");
        }
    }

    private System.Collections.IEnumerator DelayedCompleteUIUpdate()
    {
        Debug.Log("[GameManager] DelayedCompleteUIUpdate started");
        yield return new WaitForSeconds(0.3f); // RPC 전송 시간 대기

        if (Runner.IsServer)
        {
            Debug.Log("[GameManager] Calling Rpc_UpdatePlayerUI from DelayedCompleteUIUpdate");
            Rpc_UpdatePlayerUI(); // 기존 RPC 사용
        }
    }
}