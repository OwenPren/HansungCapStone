using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;

[System.Serializable]
public class PlayerStock
{
    public string stockName;
    public int quantity;
    public float usedMoney;
    public float stockReturn;
}

[System.Serializable]
public struct NetworkPlayerStock : INetworkStruct
{
    public NetworkString<_32> stockName;
    public int quantity;
    public float usedMoney;
    public float stockReturn;

    public NetworkPlayerStock(string name, int qty, float money, float returnVal)
    {
        stockName = name;
        quantity = qty;
        usedMoney = money;
        stockReturn = returnVal;
    }
}

public class PlayerManager : NetworkBehaviour
{
    [Networked] public float playerCash { get; private set; }
    [Networked] public float playerValue { get; private set; }
    [Networked] public float previousValue { get; private set; }
    [Networked] public float portfolioReturn { get; private set; }
    public PlayerRef PlayerRef { get; private set; }
    public string NameField;
    public List<PlayerStock> portfolio = new List<PlayerStock>();
    // OnChanged 콜백을 사용하여 포트폴리오 변경 감지
    [Networked, Capacity(10), OnChangedRender(nameof(OnPortfolioChanged))]
    public NetworkArray<NetworkPlayerStock> NetworkedPortfolio { get; }
    public Sprite character;
    public StockMarketManager stockMarketManager;

    // Spawned 상태 확인
    public bool IsSpawned { get; private set; } = false;

    // 초기화 완료 플래그
    private bool isInitialized = false;

    public override void Spawned()
    {
        IsSpawned = true;

        // PlayerRef 자동 설정
        if (PlayerRef == default(PlayerRef))
        {
            if (Object.HasInputAuthority)
            {
                PlayerRef = Runner.LocalPlayer;
                Debug.Log($"[PlayerManager] Auto-assigned PlayerRef to LocalPlayer: {PlayerRef}");
            }
            else
            {
                PlayerRef = Object.InputAuthority;
                Debug.Log($"[PlayerManager] Auto-assigned PlayerRef to InputAuthority: {PlayerRef}");
            }
        }

        Debug.Log($"[PlayerManager] PlayerManager spawned for player {PlayerRef} on {(Runner.IsServer ? "SERVER" : "CLIENT")}");

        // 클라이언트에서 약간의 지연 후 네트워크 포트폴리오 동기화
        if (!Runner.IsServer)
        {
            Invoke(nameof(SyncPortfolioFromNetwork), 2f);
        }

        StartCoroutine(RegisterWithGameManagerDelayed());
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsSpawned = false;
        isInitialized = false;
        Debug.Log($"[PlayerManager] PlayerManager despawned for player {PlayerRef}");

        // GameManager에서 제거
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayerManager(PlayerRef);
        }
    }

    // GameManager에 등록하는 코루틴 (재시도 로직 포함)
    private System.Collections.IEnumerator RegisterWithGameManager()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayerManager(PlayerRef, this);
                Debug.Log($"[PlayerManager] Successfully registered with GameManager for player {PlayerRef}");
                break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError($"[PlayerManager] Failed to register with GameManager for player {PlayerRef} - timeout!");
        }
    }

    private System.Collections.IEnumerator RegisterWithGameManagerDelayed()
    {
        float timeout = Runner.IsServer ? 5f : 15f;
        float elapsed = 0f;
        float retryInterval = 0.5f;

        Debug.Log($"[PlayerManager] Starting registration process for PlayerRef: {PlayerRef} on {(Runner.IsServer ? "SERVER" : "CLIENT")}");

        // PlayerRef가 None인 경우 재시도
        while (PlayerRef == default(PlayerRef) && elapsed < 5f)
        {
            Debug.LogWarning($"[PlayerManager] PlayerRef is None, attempting to reassign...");

            if (Object.HasInputAuthority)
            {
                PlayerRef = Runner.LocalPlayer;
                Debug.Log($"[PlayerManager] Reassigned PlayerRef to LocalPlayer: {PlayerRef}");
            }
            else if (Object.InputAuthority != default(PlayerRef))
            {
                PlayerRef = Object.InputAuthority;
                Debug.Log($"[PlayerManager] Reassigned PlayerRef to InputAuthority: {PlayerRef}");
            }

            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (PlayerRef == default(PlayerRef))
        {
            Debug.LogError($"[PlayerManager] Failed to assign valid PlayerRef after retries!");
            yield break;
        }

        // GameManager 등록 시도
        elapsed = 0f;
        while (elapsed < timeout)
        {
            if (GameManager.Instance != null)
            {
                Debug.Log($"[PlayerManager] GameManager found, attempting registration for {PlayerRef}");
                GameManager.Instance.RegisterPlayerManager(PlayerRef, this);

                // 등록 성공 확인
                var registeredManager = GameManager.Instance.GetPlayerManager(PlayerRef);
                if (registeredManager == this)
                {
                    Debug.Log($"[PlayerManager] Successfully registered with GameManager for player {PlayerRef} on {(Runner.IsServer ? "SERVER" : "CLIENT")}");

                    // 초기화 확인 및 실행
                    if (GetPlayerCash() == 0 || portfolio.Count == 0)
                    {
                        Initialize(50000000f);
                        Debug.Log($"[PlayerManager] Initialized PlayerManager for {PlayerRef}");
                    }

                    // PlayerInfo 동기화
                    SyncPlayerInfo();

                    // 클라이언트에서 등록 성공 시 UI 업데이트 요청
                    if (!Runner.IsServer)
                    {
                        StartCoroutine(RequestUIUpdateDelayed());
                    }
                    break;
                }
                else
                {
                    Debug.LogWarning($"[PlayerManager] Registration verification failed for {PlayerRef}");
                }
            }

            yield return new WaitForSeconds(retryInterval);
            elapsed += retryInterval;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError($"[PlayerManager] Failed to register with GameManager for player {PlayerRef} after {timeout} seconds on {(Runner.IsServer ? "SERVER" : "CLIENT")}");
        }
    }

    // 포트폴리오 동기화 메서드
    private void SyncPortfolioFromNetwork()
    {
        if (!IsSpawned) return;

        // 네트워크 포트폴리오에서 로컬 포트폴리오로 복사
        portfolio.Clear();

        for (int i = 0; i < NetworkedPortfolio.Length; i++)
        {
            var networkStock = NetworkedPortfolio[i];
            if (!string.IsNullOrEmpty(networkStock.stockName.ToString()))
            {
                portfolio.Add(new PlayerStock
                {
                    stockName = networkStock.stockName.ToString(),
                    quantity = networkStock.quantity,
                    usedMoney = networkStock.usedMoney,
                    stockReturn = networkStock.stockReturn
                });
            }
        }

        Debug.Log($"[PlayerManager] Synced portfolio from network: {portfolio.Count} stocks");

        // 변경된 포트폴리오 내용 로깅
        foreach (var stock in portfolio)
        {
            if (stock.quantity > 0)
            {
                Debug.Log($"[PlayerManager] Portfolio sync - {stock.stockName}: {stock.quantity} shares");
            }
        }
    }
    private void SyncPortfolioToNetwork()
    {
        if (!IsSpawned || !Object.HasStateAuthority) return;

        Debug.Log($"[PlayerManager] Syncing {portfolio.Count} stocks to network");

        // 로컬 포트폴리오에서 네트워크 포트폴리오로 복사
        for (int i = 0; i < portfolio.Count && i < NetworkedPortfolio.Length; i++)
        {
            var localStock = portfolio[i];
            NetworkedPortfolio.Set(i, new NetworkPlayerStock(
                localStock.stockName,
                localStock.quantity,
                localStock.usedMoney,
                localStock.stockReturn
            ));

            if (localStock.quantity > 0)
            {
                Debug.Log($"[PlayerManager] Synced to network - {localStock.stockName}: {localStock.quantity} shares");
            }
        }

        // 나머지 슬롯 초기화
        for (int i = portfolio.Count; i < NetworkedPortfolio.Length; i++)
        {
            NetworkedPortfolio.Set(i, new NetworkPlayerStock("", 0, 0, 0));
        }

        Debug.Log($"[PlayerManager] Portfolio sync to network completed");
    }


    private void OnPortfolioChanged()
    {
        Debug.Log("[PlayerManager] NetworkedPortfolio changed, updating local portfolio");
        SyncPortfolioFromNetwork();
        UpdateInventoryUI();
    }
    private void SyncPlayerInfo()
    {
        if (PlayerInfoManager.Instance != null)
        {
            var playerInfo = PlayerInfoManager.Instance.GetPlayerInfo(PlayerRef);
            if (playerInfo.HasValue)
            {
                // 권한과 관계없이 로컬 필드는 업데이트
                NameField = playerInfo.Value.nickname.ToString();

                // State Authority가 있는 경우에만 네트워크 변수 업데이트
                if (Object.HasStateAuthority)
                {
                    UpdatePlayerInfo(
                        playerInfo.Value.userID.ToString(),
                        playerInfo.Value.nickname.ToString(),
                        playerInfo.Value.selectedCharacterIndex
                    );
                }

                Debug.Log($"[PlayerManager] Synced player info for {PlayerRef}: '{playerInfo.Value.nickname.ToString()}'");
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] No PlayerInfo found for {PlayerRef} during sync");
            }
        }
    }

    public void SetPlayerRef(PlayerRef playerRef)
    {
        if (playerRef == default(PlayerRef))
        {
            Debug.LogWarning("[PlayerManager] Attempting to set PlayerRef to None/default!");
            return;
        }

        this.PlayerRef = playerRef;
        Debug.Log($"[PlayerManager] PlayerRef set to: {playerRef}");
    }

    private System.Collections.IEnumerator RequestUIUpdateDelayed()
    {
        yield return new WaitForSeconds(2f); // UI 시스템이 준비될 때까지 대기

        // 클라이언트에서는 직접 RPC 호출하지 말고 로컬 UI만 업데이트
        if (!Runner.IsServer)
        {
            Debug.Log("[PlayerManager] Requesting local UI update from client");

            // 로컬 UI 직접 업데이트
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateCurrentRanking();
            }

            // 또는 서버에 UI 업데이트 요청 (권한 문제 해결)
            // GameManager.Instance.RpcRequestUIUpdate(); // 새로운 RPC 사용
        }
    }

    // 안전한 Networked 프로퍼티 접근 메서드들
    public float GetPlayerCash()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to access playerCash before spawned");
            return 0f;
        }
        return playerCash;
    }

    public float GetPlayerValue()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to access playerValue before spawned");
            return 0f;
        }
        return playerValue;
    }

    public float GetPreviousValue()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to access previousValue before spawned");
            return 0f;
        }
        return previousValue;
    }

    public float GetPortfolioReturn()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to access portfolioReturn before spawned");
            return 0f;
        }

        UpdatePortfolioReturn();

        return portfolioReturn;
    }

    // Networked 프로퍼티 안전한 설정 메서드들
    public void SetPlayerCash(float value)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to set playerCash before spawned");
            return;
        }
        if (Object.HasStateAuthority)
        {
            playerCash = value;
        }
    }

    public void SetPlayerValue(float value)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to set playerValue before spawned");
            return;
        }
        if (Object.HasStateAuthority)
        {
            playerValue = value;
        }
    }

    void Start()
    {
        StartCoroutine(FindStockMarketManager());
    }

    private System.Collections.IEnumerator FindStockMarketManager()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout && stockMarketManager == null)
        {
            GameObject stockMarketManagerObject = GameObject.Find("StockMarketManager");
            if (stockMarketManagerObject != null)
            {
                stockMarketManager = stockMarketManagerObject.GetComponent<StockMarketManager>();

                if (stockMarketManager != null)
                {
                    Debug.Log("[PlayerManager] StockMarketManager Find Success.");
                    break;
                }
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (stockMarketManager == null)
        {
            Debug.LogError("[PlayerManager] StockMarketManager Find Fail after timeout.");
        }
    }

    private List<string> stockNames = new List<string>
    {
        "Energy",
        "Technology",
        "Finance",
        "Healthcare",
        "ConsumerDiscretionary",
        "ConsumerStaples",
        "Telecom",
        "Industrials",
        "Materials",
        "RealEstate"
    };

    public void Initialize(float initialCash)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to initialize before spawned");
            return;
        }

        if (Object.HasStateAuthority)
        {
            playerCash = initialCash;
            playerValue = initialCash;
            previousValue = initialCash;
            portfolioReturn = 0.0f;
        }

        NameField = PlayerData.instance?.nickname ?? "Unknown";

        // 포트폴리오 초기화
        portfolio.Clear();
        foreach (string stockName in stockNames)
        {
            portfolio.Add(new PlayerStock
            {
                stockName = stockName,
                quantity = 0,
                usedMoney = 0,
                stockReturn = 0
            });
        }

        // 네트워크 포트폴리오 동기화
        if (Object.HasStateAuthority)
        {
            SyncPortfolioToNetwork();
        }

        isInitialized = true;
        Debug.Log($"[PlayerManager] PlayerManager initialized with cash: {initialCash}");
    }

    [Networked] public NetworkString<_32> NetworkedNickname { get; private set; }
    [Networked] public NetworkString<_32> NetworkedUserID { get; private set; }
    [Networked] public int NetworkedCharacterIndex { get; private set; }

    public void UpdatePlayerInfo(string userID, string nickname, int characterIndex)
    {
        Debug.Log($"[PlayerManager] UpdatePlayerInfo called - UserID: '{userID}', Nickname: '{nickname}', CharIndex: {characterIndex}");
        Debug.Log($"[PlayerManager] HasStateAuthority: {Object.HasStateAuthority}, HasInputAuthority: {Object.HasInputAuthority}");

        // State Authority가 있는 경우에만 네트워크 변수 업데이트
        if (Object.HasStateAuthority)
        {
            Debug.Log($"[PlayerManager] Has state authority, updating networked variables...");
            NetworkedNickname = nickname;
            NetworkedUserID = userID;
            NetworkedCharacterIndex = characterIndex;
            Debug.Log($"[PlayerManager] Networked variables updated successfully");
        }
        else
        {
            Debug.Log($"[PlayerManager] Does NOT have state authority, only updating local field");
        }

        // 로컬 필드는 항상 업데이트 (모든 클라이언트에서)
        NameField = nickname;
        Debug.Log($"[PlayerManager] NameField set to: '{NameField}'");

        Debug.Log($"[PlayerManager] Player info update completed for: {nickname}");
    }

    public void SetPreviousValue()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to set previousValue before spawned");
            return;
        }
        if (Object.HasStateAuthority)
        {
            previousValue = playerValue;
        }
    }

    // public void SetPlayerRef(PlayerRef playerRef)
    // {
    //     this.PlayerRef = playerRef;
    // }

    public void UpdatePortfolioReturn()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to update portfolio return before spawned");
            return;
        }

        if (Object.HasStateAuthority)
        {
            if (playerValue > 0)
            {
                portfolioReturn = (100 * (playerValue / 5000000.0f)) - 100.00f;
            }
        }
    }

    public int GetPlayerStockQuantity(string name)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"[PlayerManager] GetPlayerStockQuantity called before initialization for stock: {name}");
            return 0;
        }

        var holding = portfolio.Find(h => h.stockName == name);
        return holding != null ? holding.quantity : 0;
    }

    public void ValuationUpdate(List<PlayerStock> portfolio)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PlayerManager] Attempting to update valuation before spawned");
            return;
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[PlayerManager] Attempting to update valuation before initialization");
            return;
        }

        float StockValuation = 0f;

        if (portfolio == null)
        {
            Debug.LogError("Portfolio is null!");
            return;
        }

        foreach (PlayerStock playerStock in portfolio)
        {
            if (stockMarketManager == null)
            {
                Debug.LogWarning("StockMarketManager is null, skipping valuation update");
                continue;
            }

            StockData currentStock = stockMarketManager.GetStockData(playerStock.stockName);

            if (currentStock == null)
            {
                Debug.LogWarning($"Stock data not found for {playerStock.stockName}. Skipping.");
                continue;
            }

            float stockValue = (float)playerStock.quantity * currentStock.currentPrice;

            if (playerStock.usedMoney > 0)
            {
                playerStock.stockReturn = (100.0f * stockValue) / playerStock.usedMoney - 100.0f;
            }
            else
            {
                playerStock.stockReturn = 0.0f;
            }

            StockValuation += stockValue;

        }
        UpdatePortfolioReturn();
        if (Object.HasStateAuthority)
        {
            playerValue = StockValuation + this.playerCash;
        }
    }

    public bool BuyStock(string name, int quantity)
    {
        if (!IsSpawned || !isInitialized)
        {
            Debug.LogWarning("[PlayerManager] Attempting to buy stock before initialization");
            return false;
        }

        if (stockMarketManager == null)
        {
            Debug.LogError("StockMarketManager is null");
            return false;
        }

        StockData CurrentStock = stockMarketManager.GetStockData(name);
        if (CurrentStock == null)
        {
            Debug.Log("StockLoad Fail");
            return false;
        }

        float Price = CurrentStock.currentPrice;

        if (quantity <= 0)
        {
            return false;
        }

        float cost = quantity * Price;

        if (playerCash >= cost && Object.HasStateAuthority)
        {
            playerCash -= cost;

            var holding = portfolio.Find(h => h.stockName == name);
            if (holding != null)
            {
                int oldQuantity = holding.quantity;
                holding.quantity += quantity;
                holding.usedMoney += cost;

                Debug.Log($"[PlayerManager] Buy successful: {name} x{quantity}, quantity: {oldQuantity} -> {holding.quantity}");

                // 네트워크 포트폴리오 동기화 (이것이 OnPortfolioChanged 콜백을 트리거함)
                SyncPortfolioToNetwork();

                // 포트폴리오 가치 업데이트
                ValuationUpdate(portfolio);

                Debug.Log($"[PlayerManager] Portfolio synced to network after buy");

                // 네트워크 동기화 완료 후 UI 업데이트 요청
                StartCoroutine(RequestUIUpdateAfterSync());
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"[PlayerManager] Buy failed: Cash={playerCash:N0}, Cost={cost:N0}, HasAuthority={Object.HasStateAuthority}");
            return false;
        }
    }


    public bool SellStock(string name, int quantity)
    {
        if (!IsSpawned || !isInitialized)
        {
            Debug.LogWarning("[PlayerManager] Attempting to sell stock before initialization");
            return false;
        }

        if (stockMarketManager == null)
        {
            Debug.LogError("StockMarketManager is null");
            return false;
        }

        StockData CurrentStock = stockMarketManager.GetStockData(name);
        if (CurrentStock == null)
        {
            Debug.Log("StockLoad Fail");
            return false;
        }

        float Price = CurrentStock.currentPrice;

        if (quantity <= 0)
        {
            return false;
        }

        var holding = portfolio.Find(h => h.stockName == name);

        if (holding != null && holding.quantity >= quantity && Object.HasStateAuthority)
        {
            float revenue = quantity * Price;
            playerCash += revenue;
            holding.usedMoney -= (((float)quantity / holding.quantity) * holding.usedMoney);

            int oldQuantity = holding.quantity;
            holding.quantity -= quantity;

            Debug.Log($"[PlayerManager] Sell successful: {name} x{quantity}, quantity: {oldQuantity} -> {holding.quantity}");

            // 네트워크 포트폴리오 동기화
            SyncPortfolioToNetwork();

            // 포트폴리오 가치 업데이트
            ValuationUpdate(portfolio);

            Debug.Log($"[PlayerManager] Portfolio synced to network after sell");

            // 네트워크 동기화 완료 후 UI 업데이트 요청
            StartCoroutine(RequestUIUpdateAfterSync());

            return true;
        }
        else
        {
            Debug.LogWarning($"[PlayerManager] Sell failed: Have={holding?.quantity ?? 0}, Want={quantity}, HasAuthority={Object.HasStateAuthority}");
            return false;
        }
    }

    private System.Collections.IEnumerator RequestUIUpdateAfterSync()
    {
        // 네트워크 동기화가 완료될 시간을 주기 위해 짧은 지연
        yield return new WaitForSeconds(0.05f);

        // 로컬 UI 직접 업데이트
        UpdateInventoryUI();

        // MarketPanel2UI가 열려있다면 새로고침
        MarketPanel2UI marketPanel = FindObjectOfType<MarketPanel2UI>();
        if (marketPanel != null && marketPanel.gameObject.activeInHierarchy)
        {
            marketPanel.RefreshPlayerHolding();
        }
    }

    private void UpdateInventoryUI()
    {
        Debug.Log("[PlayerManager] UpdateInventoryUI called");

        // MarketPanel2UI가 활성화되어 있다면 업데이트
        MarketPanel2UI marketPanel = FindObjectOfType<MarketPanel2UI>();
        if (marketPanel != null && marketPanel.gameObject.activeInHierarchy)
        {
            Debug.Log("[PlayerManager] Refreshing MarketPanel2UI");
            marketPanel.RefreshPlayerHolding();
        }

        Debug.Log("[PlayerManager] Inventory UI update completed");
    }

}