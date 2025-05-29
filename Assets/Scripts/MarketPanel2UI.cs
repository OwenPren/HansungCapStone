using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Linq;
using System.Collections;

public class MarketPanel2UI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI stockNameText;
    public TextMeshProUGUI currentPriceText;
    public TextMeshProUGUI playerHoldingText;
    public TMP_InputField quantityInput;
    public Button buyButton;
    public Button sellButton;
    public Button closeButton;
    public Button incrementButton; // + 버튼
    public Button decrementButton; // - 버튼

    private string currentStockName;
    private PlayerManager localPlayerManager;
    private bool isSearchingForPlayer = false;

    public AudioClip sellclickSound;    // 매수 버튼 클릭음
    public AudioClip buyclickSound;    // 매수 버튼 클릭음
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // MarketPanel2UI가 활성화될 때마다 로컬 플레이어의 PlayerManager를 찾습니다.
    void OnEnable()
    {
        StartCoroutine(FindLocalPlayerManagerCoroutine());

        // 주기적으로 UI 업데이트 (네트워크 지연 대응)
        StartCoroutine(PeriodicUpdate());
    }

    void OnDisable()
    {
        // 필요시 정리 작업
        StopAllCoroutines();
        isSearchingForPlayer = false;
    }

    IEnumerator FindLocalPlayerManagerCoroutine()
    {
        if (isSearchingForPlayer) yield break;

        isSearchingForPlayer = true;
        localPlayerManager = null;

        Debug.Log("[MarketPanel2UI] Starting search for local PlayerManager...");

        float timeout = 10f; // 10초 타임아웃
        float elapsed = 0f;

        while (elapsed < timeout && localPlayerManager == null)
        {
            yield return new WaitForSeconds(0.1f); // 0.1초마다 체크
            elapsed += 0.1f;

            FindLocalPlayerManager();

            if (localPlayerManager != null && localPlayerManager.IsSpawned)
            {
                Debug.Log("[MarketPanel2UI] Local PlayerManager found successfully!");
                break;
            }
        }

        if (localPlayerManager == null)
        {
            Debug.LogWarning("[MarketPanel2UI] Could not find local PlayerManager within timeout period!");
        }

        isSearchingForPlayer = false;

        // 초기 보유량 업데이트
        UpdatePlayerHolding();
    }

    void FindLocalPlayerManager()
    {
        if (localPlayerManager != null && localPlayerManager.IsSpawned) return;

        // 방법 1: NetworkRunner를 통해 로컬 플레이어 찾기
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && GameManager.Instance != null)
        {
            PlayerRef localPlayer = runner.LocalPlayer;
            var tempManager = GameManager.Instance.GetPlayerManager(localPlayer);

            if (tempManager != null && tempManager.IsSpawned)
            {
                localPlayerManager = tempManager;
                Debug.Log($"[MarketPanel2UI] Local PlayerManager found via GameManager for player {localPlayer}!");
                return;
            }
        }

        // 방법 2: FindObjectsOfType으로 모든 PlayerManager 확인
        PlayerManager[] allPlayerManagers = FindObjectsOfType<PlayerManager>();

        foreach (PlayerManager pm in allPlayerManagers)
        {
            if (pm != null && pm.IsSpawned)
            {
                // 로컬 플레이어인지 확인 (Input Authority 체크)
                if (pm.Object != null && pm.Object.HasInputAuthority)
                {
                    localPlayerManager = pm;
                    Debug.Log($"[MarketPanel2UI] Local PlayerManager found via HasInputAuthority!");
                    return;
                }

                // Runner.LocalPlayer와 비교
                if (runner != null && pm.PlayerRef == runner.LocalPlayer)
                {
                    localPlayerManager = pm;
                    Debug.Log($"[MarketPanel2UI] Local PlayerManager found via PlayerRef comparison!");
                    return;
                }
            }
        }
    }

    public void DisplayStockInfo(string name, string nameKR)
    {
        gameObject.SetActive(true);
        currentStockName = name;

        // PlayerManager가 없다면 다시 찾기 시도
        if (localPlayerManager == null || !localPlayerManager.IsSpawned)
        {
            StartCoroutine(FindLocalPlayerManagerCoroutine());
        }

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.stockMarketManager == null)
        {
            Debug.LogError("[MarketPanel2UI] GameManager or StockMarketManager is null!");
            gameObject.SetActive(false);
            return;
        }

        StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
        if (stock != null)
        {
            stockNameText.text = nameKR;
            currentPriceText.text = "현재가: " + stock.currentPrice.ToString("N2");

            // 로컬 플레이어의 보유량 표시
            UpdatePlayerHolding();
        }
        else
        {
            stockNameText.text = "Error";
            currentPriceText.text = "N/A";
            playerHoldingText.text = "보유량: N/A";
        }

        quantityInput.text = "1";
    }

    private void UpdatePlayerHolding()
    {
        if (string.IsNullOrEmpty(currentStockName))
        {
            playerHoldingText.text = "보유량: 선택된 종목 없음";
            return;
        }

        // PlayerManager가 없거나 Spawn되지 않은 경우 재검색
        if (localPlayerManager == null || !localPlayerManager.IsSpawned)
        {
            FindLocalPlayerManager(); // 즉시 재검색 시도
        }

        if (localPlayerManager != null && localPlayerManager.IsSpawned)
        {
            int holding = localPlayerManager.GetPlayerStockQuantity(currentStockName);
            playerHoldingText.text = "보유 현황: " + holding.ToString() + " 개 보유중";
            Debug.Log($"[MarketPanel2UI] Updated holding for {currentStockName}: {holding}");

            // 추가: 현재 주가도 함께 업데이트
            if (GameManager.Instance?.stockMarketManager != null)
            {
                StockData stock = GameManager.Instance.stockMarketManager.GetStockData(currentStockName);
                if (stock != null)
                {
                    currentPriceText.text = "현재가: " + stock.currentPrice.ToString("N2");
                }
            }
        }
        else
        {
            playerHoldingText.text = "보유량: 조회중... (플레이어 정보 로딩)";

            // PlayerManager가 없다면 다시 찾기 시도
            if (!isSearchingForPlayer)
            {
                StartCoroutine(FindLocalPlayerManagerCoroutine());
            }
        }
    }

    public void OnIncrementButtonClick()
    {
        if (quantityInput == null) return;

        if (int.TryParse(quantityInput.text, out int currentQuantity))
        {
            currentQuantity++;
            quantityInput.text = currentQuantity.ToString();
            Debug.Log("Quantity incremented to: " + currentQuantity);
        }
        else
        {
            quantityInput.text = "1";
            Debug.LogWarning("Invalid quantity input, setting to 1.");
        }
    }

    public void OnDecrementButtonClick()
    {
        if (quantityInput == null) return;

        if (int.TryParse(quantityInput.text, out int currentQuantity))
        {
            currentQuantity = Mathf.Max(1, currentQuantity - 1);
            quantityInput.text = currentQuantity.ToString();
            Debug.Log("Quantity decremented to: " + currentQuantity);
        }
        else
        {
            quantityInput.text = "1";
            Debug.LogWarning("Invalid quantity input, setting to 1.");
        }
    }

    public void OnBuyButtonClick()
    {
        // 클릭 사운드 재생
        if (buyclickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buyclickSound);
        }
        Debug.Log($"[MarketPanel2UI] Buy button clicked for stock: {currentStockName}");

        if (string.IsNullOrEmpty(currentStockName))
        {
            Debug.LogError("[MarketPanel2UI] No stock selected for buying");
            return;
        }

        if (!int.TryParse(quantityInput.text, out int quantity) || quantity <= 0)
        {
            Debug.LogWarning("[MarketPanel2UI] Invalid quantity entered for buying");
            return;
        }

        // PlayerManager 유효성 재검사
        if (localPlayerManager == null || !localPlayerManager.IsSpawned)
        {
            Debug.LogWarning("[MarketPanel2UI] Local PlayerManager not available, retrying...");
            StartCoroutine(RetryAfterPlayerManagerFound(() => OnBuyButtonClick()));
            return;
        }

        // 로컬 플레이어의 PlayerRef 찾기
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[MarketPanel2UI] NetworkRunner not found!");
            return;
        }

        PlayerRef myPlayerRef = runner.LocalPlayer;
        Debug.Log($"[MarketPanel2UI] My PlayerRef: {myPlayerRef}");

        // RPC를 통해 구매 요청 전송
        if (GameManager.Instance != null)
        {
            Debug.Log($"[MarketPanel2UI] Sending buy request via RPC - Player: {myPlayerRef}, Stock: {currentStockName}, Quantity: {quantity}");
            GameManager.Instance.RpcBuyStockRequest(myPlayerRef, currentStockName, quantity);

            // 즉시 UI 업데이트 (네트워크 지연 고려하여 약간의 지연)
            StartCoroutine(DelayedUpdatePlayerHolding(0.5f));
        }
        else
        {
            Debug.LogError("[MarketPanel2UI] GameManager.Instance is null!");
        }
    }

    public void OnSellButtonClick()
    {
        // 클릭 사운드 재생
        if (sellclickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sellclickSound);
        }

        Debug.Log($"[MarketPanel2UI] Sell button clicked for stock: {currentStockName}");

        if (string.IsNullOrEmpty(currentStockName))
        {
            Debug.LogError("[MarketPanel2UI] No stock selected for selling");
            return;
        }

        if (!int.TryParse(quantityInput.text, out int quantity) || quantity <= 0)
        {
            Debug.LogWarning("[MarketPanel2UI] Invalid quantity entered for selling");
            return;
        }

        // PlayerManager 유효성 재검사
        if (localPlayerManager == null || !localPlayerManager.IsSpawned)
        {
            Debug.LogWarning("[MarketPanel2UI] Local PlayerManager not available, retrying...");
            StartCoroutine(RetryAfterPlayerManagerFound(() => OnSellButtonClick()));
            return;
        }

        // 보유량 체크 (클라이언트에서 미리 검증)
        int currentHolding = localPlayerManager.GetPlayerStockQuantity(currentStockName);
        if (currentHolding < quantity)
        {
            Debug.LogWarning($"[MarketPanel2UI] Insufficient stock holding. Have: {currentHolding}, Want to sell: {quantity}");
            return;
        }

        // 로컬 플레이어의 PlayerRef 찾기
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[MarketPanel2UI] NetworkRunner not found!");
            return;
        }

        PlayerRef myPlayerRef = runner.LocalPlayer;
        Debug.Log($"[MarketPanel2UI] My PlayerRef: {myPlayerRef}");

        // RPC를 통해 판매 요청 전송
        if (GameManager.Instance != null)
        {
            Debug.Log($"[MarketPanel2UI] Sending sell request via RPC - Player: {myPlayerRef}, Stock: {currentStockName}, Quantity: {quantity}");
            GameManager.Instance.RpcSellStockRequest(myPlayerRef, currentStockName, quantity);

            // 즉시 UI 업데이트 (네트워크 지연 고려하여 약간의 지연)
            StartCoroutine(DelayedUpdatePlayerHolding(0.5f));
        }
        else
        {
            Debug.LogError("[MarketPanel2UI] GameManager.Instance is null!");
        }
    }

    // PlayerManager를 찾을 때까지 대기한 후 액션 실행
    private IEnumerator RetryAfterPlayerManagerFound(System.Action action)
    {
        yield return StartCoroutine(FindLocalPlayerManagerCoroutine());

        if (localPlayerManager != null && localPlayerManager.IsSpawned)
        {
            action?.Invoke();
        }
        else
        {
            Debug.LogError("[MarketPanel2UI] Still could not find PlayerManager after retry");
        }
    }

    // 지연된 보유량 업데이트
    private IEnumerator DelayedUpdatePlayerHolding(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdatePlayerHolding();
    }

    public void OnCloseButtonClick()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyButtonClick);
        if (sellButton != null) sellButton.onClick.RemoveListener(OnSellButtonClick);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseButtonClick);
    }

    public void RefreshPlayerHolding()
    {
        if (!gameObject.activeInHierarchy) return;

        Debug.Log("[MarketPanel2UI] RefreshPlayerHolding called");

        // 약간의 지연 후 업데이트 (네트워크 동기화 대기)
        StartCoroutine(DelayedRefresh());
    }

    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(0.1f); // 네트워크 동기화 대기
        UpdatePlayerHolding();
    }
    private System.Collections.IEnumerator PeriodicUpdate()
    {
        while (gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(0.5f); // 0.5초마다 업데이트

            if (!string.IsNullOrEmpty(currentStockName))
            {
                UpdatePlayerHolding();
            }
        }
    }


}