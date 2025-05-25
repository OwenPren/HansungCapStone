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

    [Networked, Capacity(10)] public NetworkArray<NetworkString<_256>> NetworkedHintData { get; }
    public List<string> HintData = new List<string>();
    public Dictionary<string, string> UpdateSectorImpacts = new Dictionary<string, string>();
    public Dictionary<string, string> SectorImpacts = new Dictionary<string, string>();

    private bool isWaiting = false;
    public float waitTimer = 10.0f;
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
        if (CurrentRound == 0)
        {
            // 모든 클라이언트에 랭킹 업데이트 알림
            RpcUpdateCurrentRanking();
        }

        // 모든 클라이언트에 UI 업데이트 알림
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

        roundStartEvent.Raise(this);
    }

    void EndRound(bool start)
    {
        State = GameState.Ended;

        if (!start)
        {
            UpdateStockPrices(UpdateSectorImpacts);

            // 모든 클라이언트에 랭킹 업데이트 알림
            RpcUpdateCurrentRanking();
        }

        // 모든 클라이언트에 결과 UI 업데이트 알림
        RpcUpdateResultUI();
        RpcShowResultPanel(true);

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
        }
        else
        {
            Debug.Log("[GameManager] (CLIENT) Game Started - starting sync check");
            StartClientSyncCheck();
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
        Debug.Log("[GameManager] RpcUpdateCurrentRanking received");

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
                
                // PlayerManager에 정보 업데이트
                manager.UpdatePlayerInfo(
                    playerInfo.Value.userID.ToString(),
                    playerInfo.Value.nickname.ToString(),
                    playerInfo.Value.selectedCharacterIndex
                );
            }
            else
            {
                Debug.LogWarning($"[GameManager] (CLIENT) No PlayerInfo found for {playerRef}");
            }
        }
        
        // 약간의 지연 후 UI 업데이트 요청
        StartCoroutine(DelayedUIUpdate());
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
            // PlayerManager가 유효한지 확인
            if (playerManager == null || !playerManager.IsSpawned)
            {
                Debug.LogError($"[GameManager] PlayerManager for {sender} is null or not spawned");
                return;
            }

            // PlayerManager의 BuyStock 로직 실행
            bool success = playerManager.BuyStock(stockName, quantity);
            Debug.Log($"[GameManager] Buy Request from {sender}: {(success ? "SUCCESS" : "FAILED")}");

            // 성공 여부와 관계없이 UI 업데이트는 클라이언트에서 처리
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
            // PlayerManager가 유효한지 확인
            if (playerManager == null || !playerManager.IsSpawned)
            {
                Debug.LogError($"[GameManager] PlayerManager for {sender} is null or not spawned");
                return;
            }

            StockData stock = stockMarketManager?.GetStockData(stockName);
            if (stock != null && stock.currentPrice > 0)
            {
                // PlayerManager의 SellStock 로직 실행
                bool success = playerManager.SellStock(stockName, quantity);
                Debug.Log($"[GameManager] Sell Request from {sender}: {(success ? "SUCCESS" : "FAILED")}");

                // 성공 여부와 관계없이 UI 업데이트는 클라이언트에서 처리
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

                if (playerManager != null && playerManager.IsSpawned)
                {
                    playerManager.ValuationUpdate(playerManager.portfolio);
                    Debug.Log($"플레이어의 포트폴리오 가치 업데이트 완료.");
                }
                else
                {
                    Debug.LogWarning($"특정 플레이어의 PlayerManager가 null이거나 spawned되지 않았습니다.");
                }
            }
        }

        // 3. UI 업데이트
        if (UIManager != null)
        {
            UIManager.UpdateCurrentCashandValue();
        }

        UpdateSectorImpacts = SectorImpacts;
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
    
}