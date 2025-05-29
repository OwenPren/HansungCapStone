using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Fusion;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gamePanel;
    public GameObject inventoryPanel;
    public GameObject marketPanel;
    public GameObject marketPanel2;
    public GameObject resultPanel;

    [Header("Player Stats UI")]
    public TextMeshProUGUI currentCashText;
    public TextMeshProUGUI currentValueText;

    [Header("Game Info UI")]
    public TextMeshProUGUI currentTimeText;
    public TextMeshProUGUI currentRoundText;

    [Header("Timer Data")]
    public TextMeshProUGUI nextTimer;
    public TextMeshProUGUI finishTimer;

    [Header("Game Rank UI")]
    public List<TextMeshProUGUI> currentRankText = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> currentRankNameText = new List<TextMeshProUGUI>();

    [Header("Hint UI")]
    public List<TextMeshProUGUI> currentHintText = new List<TextMeshProUGUI>();

    [Header("Result UI")]
    public TextMeshProUGUI ResultTitle;
    public List<TextMeshProUGUI> currentResultName = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> currentResultValue = new List<TextMeshProUGUI>();
    public List<Image> currentRankImage = new List<Image>();

    [Header("Market UI")]
    public List<Button> currentStockCount = new List<Button>();
    public List<Button> currentStockData = new List<Button>();

    private PlayerManager localPlayerManager;

    private Dictionary<string, string> stockNameMapping = new Dictionary<string, string>()
    {
        { "Energy", "에너지" },
        { "Technology", "기술" },
        { "Finance", "금융" },
        { "Healthcare", "의료" },
        { "ConsumerDiscretionary", "임의소비재" },
        { "Industrials", "산업재" },
        { "Telecom", "통신" },
        { "RealEstate", "부동산" },
        { "Materials", "소재" },
        { "ConsumerStaples", "필수소비재" }
    };


    void Update()
    {
        // 타이머 업데이트
        if (GameManager.Instance != null && currentTimeText != null)
        {
            if (GameManager.Instance.State == GameState.Ended || GameManager.Instance.State == GameState.Finished)
            {
                int remainingTime = (int)GameManager.Instance.waitTimer;
                currentTimeText.text = remainingTime.ToString();

                if (GameManager.Instance.State == GameState.Ended)
                {
                    nextTimer.gameObject.SetActive(true);
                }
                else finishTimer.gameObject.SetActive(true);
            }
            else
            {
                int remainingTime = (int)GameManager.Instance.Timer;
                currentTimeText.text = remainingTime.ToString();
                finishTimer.gameObject.SetActive(false);
                nextTimer.gameObject.SetActive(false);
            }
        }

        // 라운드 업데이트
        if (GameManager.Instance != null && currentRoundText != null)
        {
            currentRoundText.text = GameManager.Instance.CurrentRound.ToString() + "라운드";
        }

        // 플레이어 현금/가치 업데이트 (1초마다만)
        if (Time.time - lastUpdateTime > 1f)
        {
            UpdateCurrentCashandValue();
            lastUpdateTime = Time.time;
        }
    }

    private float lastUpdateTime = 0f;

    // ============= 주식 창 업데이트 메서드 =====================

    public void UpdateMarketStockUI()
    {
        Debug.Log("[UIManager] UpdateMarketStockUI called");

        if (GameManager.Instance == null)
        {
            Debug.LogError("[UIManager] GameManager.Instance is null. Cannot update market UI.");
            return;
        }

        if (localPlayerManager == null)
        {
            FindPortfolio();
            if (localPlayerManager == null)
            {
                Debug.LogWarning("[UIManager] Local PlayerManager not found yet. Market UI might not show accurate stock counts.");
            }
        }

        // 주가 정보 업데이트 (개선된 로직)
        foreach (Button stockButton in currentStockData)
        {
            if (stockButton == null || stockButton.GetComponentInChildren<TextMeshProUGUI>() == null) continue;

            string stockTag = stockButton.tag;
            if (string.IsNullOrEmpty(stockTag)) continue;

            string stockEnglishName = stockTag;
            string stockKoreanName = stockNameMapping.ContainsKey(stockEnglishName) ? stockNameMapping[stockEnglishName] : stockEnglishName;

            StockData currentStockData = GameManager.Instance.stockMarketManager.GetStockData(stockEnglishName);
            if (currentStockData != null)
            {
                // 클라이언트에서도 변동률 계산 (서버에서 계산되지 않은 경우)
                if (Mathf.Abs(currentStockData.stockChangeRate) < 0.001f && currentStockData.previousPrice > 0)
                {
                    currentStockData.stockChangeRate = (100.0f * currentStockData.currentPrice) / currentStockData.previousPrice - 100.0f;
                    Debug.Log($"[UIManager] Recalculated change rate for {stockEnglishName}: {currentStockData.stockChangeRate:F2}%");
                }

                string sign = currentStockData.stockChangeRate >= 0 ? "<color=#FF0000>(+" : "<color=#0000FF>(";
                if (currentStockData.stockChangeRate < 0.001f && currentStockData.stockChangeRate > -0.001f)
                {
                    sign = "(+";
                }

                stockButton.GetComponentInChildren<TextMeshProUGUI>().text =
                    $"한성 {stockKoreanName}\n" +
                    $"{sign}{currentStockData.stockChangeRate:F2}%)</color> " +
                    $"{currentStockData.currentPrice:F1}원";

                Debug.Log($"[UIManager] Updated {stockEnglishName} display: {currentStockData.stockChangeRate:F2}%, {currentStockData.currentPrice:F1}원");
            }
            else
            {
                stockButton.GetComponentInChildren<TextMeshProUGUI>().text =
                    $"한성 {stockKoreanName}\n" +
                    "데이터 없음";
                Debug.LogWarning($"[UIManager] Stock data not found for: {stockEnglishName}");
            }
        }

        // 보유량 정보 업데이트
        foreach (Button countButton in currentStockCount)
        {
            if (countButton == null || countButton.GetComponentInChildren<TextMeshProUGUI>() == null) continue;

            string stockTag = countButton.tag;
            if (string.IsNullOrEmpty(stockTag)) continue;

            string stockEnglishName = stockTag;
            int quantity = GetLocalPlayerStockQuantity(stockEnglishName);

            var tmpComponent = countButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpComponent != null)
            {
                tmpComponent.text = $"{quantity}주";
            }
        }

        Debug.Log("[UIManager] Market UI update completed");
    }
    // ============= 구매/판매 요청 메서드 (RPC 사용) =============

    public void RequestBuyStock(string stockName, int quantity)
    {
        Debug.Log($"[UIManager] Requesting buy stock - {stockName}, quantity: {quantity}");

        // 로컬 플레이어의 PlayerRef 찾기
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[UIManager] NetworkRunner not found!");
            return;
        }

        PlayerRef myPlayerRef = runner.LocalPlayer;
        Debug.Log($"[UIManager] My PlayerRef: {myPlayerRef}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RpcBuyStockRequest(myPlayerRef, stockName, quantity);
            Debug.Log($"[UIManager] Buy request sent via RPC for player {myPlayerRef}");
        }
        else
        {
            Debug.LogError("[UIManager] GameManager.Instance is null!");
        }
    }

    public void RequestSellStock(string stockName, int quantity)
    {
        Debug.Log($"[UIManager] Requesting sell stock - {stockName}, quantity: {quantity}");

        // 로컬 플레이어의 PlayerRef 찾기
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[UIManager] NetworkRunner not found!");
            return;
        }

        PlayerRef myPlayerRef = runner.LocalPlayer;
        Debug.Log($"[UIManager] My PlayerRef: {myPlayerRef}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RpcSellStockRequest(myPlayerRef, stockName, quantity);
            Debug.Log($"[UIManager] Sell request sent via RPC for player {myPlayerRef}");
        }
        else
        {
            Debug.LogError("[UIManager] GameManager.Instance is null!");
        }
    }

    // ============= UI 업데이트 메서드들 =============

    public void UpdateResultUI()
    {
        // 결과 타이틀 업데이트
        if (currentRoundText != null && GameManager.Instance.CurrentRound != 12)
        {
            ResultTitle.text = currentRoundText.text + " 결과";
        }
        else
        {
            ResultTitle.text = "최종 결과";
        }

        // UI 텍스트 리스트들이 올바로 할당되고 충분한 칸이 있는지 확인 
        if (currentResultName == null || currentResultValue == null ||
            currentResultName.Count < 4 || currentResultValue.Count < 4)
        {
            Debug.LogError("결과 UI 텍스트 리스트가 Inspector에서 올바로 할당되지 않았거나 크기가 4 미만입니다. 순위를 표시할 수 없습니다.");
            return;
        }

        List<(int Rank, PlayerRef PlayerRef, PlayerManager PlayerManager)> rankedInfo = null;
        if (GameManager.Instance != null)
        {
            rankedInfo = GameManager.Instance.GetRankedPlayers();
            if (rankedInfo == null)
            {
                Debug.LogWarning("GameManager.GetRankedPlayers() 함수가 null을 반환했습니다.");
                rankedInfo = new List<(int, PlayerRef, PlayerManager)>();
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance가 null입니다. 랭킹 데이터를 가져올 수 없습니다. UI를 기본값으로 채웁니다.");
            rankedInfo = new List<(int, PlayerRef, PlayerManager)>();
        }

        // 최대 4개의 텍스트 항목을 순회하며 업데이트
        for (int i = 0; i < 4; i++)
        {
            TextMeshProUGUI valueTxt = currentResultValue[i];
            TextMeshProUGUI nameTxt = currentResultName[i];
            Image resultImage = currentRankImage[i];

            if (valueTxt == null || nameTxt == null)
            {
                Debug.LogWarning($"랭킹 UI 텍스트 객체 (인덱스 {i}) 중 일부가 Inspector에서 할당되지 않았습니다.");
                continue;
            }

            if (i < rankedInfo.Count)
            {
                var playerRankInfo = rankedInfo[i];
                PlayerManager player = playerRankInfo.PlayerManager;

                if (player != null && player.IsSpawned)
                {
                    // 플레이어 이름을 PlayerInfoManager에서 가져오기
                    string sign = player.GetPortfolioReturn() >= 0 ? "<color=#FF0000>( +" : "<color=#0000FF>( ";
                    if (player.GetPortfolioReturn() < 0.001f && player.GetPortfolioReturn() > -0.0001f)
                    {
                        sign = "(+";
                    }
                    valueTxt.text = sign + player.GetPortfolioReturn().ToString("F2") + "% )";
                    nameTxt.text = player.NameField;
                    string resourcePath = "Characters/Character_" + player.NetworkedCharacterIndex.ToString();
                    Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
                    if (loadedSprite != null)
                    {
                        // resultImage 컴포넌트의 sprite 속성을 로드된 Sprite로 변경합니다.
                        resultImage.sprite = loadedSprite;
                        Debug.Log($"Successfully changed character image to: {resourcePath}");
                    }
                    if (resultImage != null) resultImage.gameObject.SetActive(true);
                }
                else
                {
                    valueTxt.gameObject.SetActive(false);
                    nameTxt.gameObject.SetActive(false);
                    if (resultImage != null) resultImage.gameObject.SetActive(false);
                    Debug.LogWarning($"rankedInfo[{i}]의 PlayerManager 객체가 null이거나 spawned되지 않았습니다.");
                }
            }
            else
            {
                // 순위에 포함되지 않은 항목은 칸을 숨김 
                valueTxt.gameObject.SetActive(false);
                nameTxt.gameObject.SetActive(false);
                if (resultImage != null) resultImage.gameObject.SetActive(false);
            }
        }

        Debug.Log("랭킹 UI 표시 업데이트 완료.");
    }

    public void UpdateHintUI(List<string> hintData)
    {
        Debug.Log($"[UIManager] UpdateHintUI called with {hintData.Count} hints");

        // 각 힌트 내용 로깅
        for (int i = 0; i < hintData.Count; i++)
        {
            Debug.Log($"[UIManager] Hint {i + 1}: '{hintData[i]}'");
        }

        // hint_1과 hint_2 컴포넌트 찾기
        TextMeshProUGUI hint1Text = GameObject.Find("hint_1")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI hint2Text = GameObject.Find("hint_2")?.GetComponent<TextMeshProUGUI>();

        if (hint1Text == null)
        {
            Debug.LogError("[UIManager] hint_1 TextMeshProUGUI component not found!");
        }

        if (hint2Text == null)
        {
            Debug.LogError("[UIManager] hint_2 TextMeshProUGUI component not found!");
        }

        // 힌트 1 업데이트
        if (hint1Text != null)
        {
            if (hintData.Count > 0)
            {
                string formattedHint1 = $"힌트 1: {hintData[0]}";
                hint1Text.text = formattedHint1;
                Debug.Log($"[UIManager] Updated hint_1 to: '{formattedHint1}'");
            }
            else
            {
                hint1Text.text = "힌트 1: ";
                Debug.Log("[UIManager] Cleared hint_1 (no hint data)");
            }
        }

        // 힌트 2 업데이트
        if (hint2Text != null)
        {
            if (hintData.Count > 1)
            {
                string formattedHint2 = $"힌트 2: {hintData[1]}";
                hint2Text.text = formattedHint2;
                Debug.Log($"[UIManager] Updated hint_2 to: '{formattedHint2}'");
            }
            else
            {
                hint2Text.text = "힌트 2: ";
                Debug.Log("[UIManager] Cleared hint_2 (no second hint)");
            }
        }

        // 3개 이상의 힌트가 있는 경우 경고
        if (hintData.Count > 2)
        {
            Debug.LogWarning($"[UIManager] Received {hintData.Count} hints, but only displaying first 2");
        }

        Debug.Log("[UIManager] Hint UI update completed");
    }

    public void UpdateCurrentRanking()
    {
        Debug.Log("[UIManager] UpdateCurrentRanking called");

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UIManager] GameManager.Instance is null in UpdateCurrentRanking");
            return;
        }

        try
        {
            var rankedPlayers = GameManager.Instance.GetRankedPlayersWithInfo();

            Debug.Log($"[UIManager] Retrieved {rankedPlayers.Count} ranked players for display");

            if (rankedPlayers.Count == 0)
            {
                Debug.LogWarning("[UIManager] No ranked players available for display");
                ClearAllRankingSlots();
                return;
            }

            // 실제 UI 업데이트
            //UpdateRankingDisplay(rankedPlayers);
            UpdateRankingDisplayCached(rankedPlayers);
            Debug.Log("[UIManager] 랭킹 UI 표시 업데이트 완료.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] Error in UpdateCurrentRanking: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ClearAllRankingSlots()
    {
        Debug.Log("[UIManager] Clearing all ranking slots");

        TextMeshProUGUI name1 = GameObject.Find("name 1")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI name2 = GameObject.Find("name 2")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI name3 = GameObject.Find("name 3")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI name4 = GameObject.Find("name 4")?.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI[] nameTexts = { name1, name2, name3, name4 };

        for (int i = 0; i < nameTexts.Length; i++)
        {
            if (nameTexts[i] != null)
            {
                nameTexts[i].text = $"name {i + 1}"; // 기본값으로 복원
            }
        }
    }

    private void UpdateRankingDisplay(List<(PlayerRef playerRef, PlayerManager manager, NetworkPlayerInfo info)> rankedPlayers)
    {
        Debug.Log($"[UIManager] UpdateRankingDisplay called with {rankedPlayers.Count} players");

        // UI 컴포넌트들 찾기 (캐시하면 더 효율적)
        TextMeshProUGUI name1 = GameObject.Find("name 1")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI name2 = GameObject.Find("name 2")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI name3 = GameObject.Find("name 3")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI name4 = GameObject.Find("name 4")?.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI[] nameTexts = { name1, name2, name3, name4 };

        // 각 순위별로 업데이트
        for (int i = 0; i < rankedPlayers.Count && i < nameTexts.Length; i++)
        {
            if (nameTexts[i] != null)
            {
                var (playerRef, manager, playerInfo) = rankedPlayers[i];
                string playerName = playerInfo.nickname.ToString();

                nameTexts[i].text = playerName;
                Debug.Log($"[UIManager] Updated rank {i + 1} to: '{playerName}' (Value: {manager.GetPlayerValue():N0})");
            }
            else
            {
                Debug.LogError($"[UIManager] name {i + 1} TextMeshProUGUI component not found!");
            }
        }

        // 남은 슬롯들 초기화
        for (int i = rankedPlayers.Count; i < nameTexts.Length; i++)
        {
            if (nameTexts[i] != null)
            {
                nameTexts[i].text = "";
            }
        }
    }

    [Header("Ranking UI Components")]
    [SerializeField] private TextMeshProUGUI[] rankingNameTexts = new TextMeshProUGUI[4];

    // Inspector에서 할당하는 경우 사용할 메서드
    private void UpdateRankingDisplayCached(List<(PlayerRef playerRef, PlayerManager manager, NetworkPlayerInfo info)> rankedPlayers)
    {
        // Inspector에서 rankingNameTexts 배열을 할당했다면 이 방법 사용
        if (rankingNameTexts != null && rankingNameTexts.Length > 0)
        {
            for (int i = 0; i < rankedPlayers.Count && i < rankingNameTexts.Length; i++)
            {
                if (rankingNameTexts[i] != null)
                {
                    var (playerRef, manager, playerInfo) = rankedPlayers[i];
                    string playerName = playerInfo.nickname.ToString();

                    rankingNameTexts[i].text = playerName;
                    Debug.Log($"[UIManager] Updated cached rank {i + 1} to: '{playerName}'");
                }
            }

            // 남은 슬롯들 초기화
            for (int i = rankedPlayers.Count; i < rankingNameTexts.Length; i++)
            {
                if (rankingNameTexts[i] != null)
                {
                    rankingNameTexts[i].text = "";
                }
            }
        }
        else
        {
            // 캐시된 배열이 없으면 기본 방법 사용
            UpdateRankingDisplay(rankedPlayers);
        }
    }

    private void DisplayPlayerRanking(int rank, PlayerRef playerRef, PlayerManager manager, NetworkPlayerInfo playerInfo)
    {
        try
        {
            // 여기에 실제 UI 업데이트 로직 구현
            // 예시:
            string nickname = playerInfo.nickname.ToString();
            float playerValue = manager.GetPlayerValue();
            float playerCash = manager.GetPlayerCash();

            // UI 컴포넌트들에 값 설정
            // rankText[rank-1].text = rank.ToString();
            // nicknameText[rank-1].text = nickname;
            // valueText[rank-1].text = playerValue.ToString("N0");
            // cashText[rank-1].text = playerCash.ToString("N0");

            Debug.Log($"[UIManager] Updated UI for rank {rank}: {nickname} - {playerValue:N0}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] Error displaying ranking for player {playerRef}: {e.Message}");
        }
    }

    private void ClearRankingDisplay()
    {
        try
        {
            // 기존 랭킹 UI 요소들 초기화
            // for (int i = 0; i < maxPlayers; i++)
            // {
            //     rankText[i].text = "";
            //     nicknameText[i].text = "";
            //     valueText[i].text = "";
            //     cashText[i].text = "";
            // }

            Debug.Log("[UIManager] Ranking display cleared");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] Error clearing ranking display: {e.Message}");
        }
    }

    private void DisplayNoPlayersMessage()
    {
        try
        {
            // "플레이어 정보 로딩 중..." 또는 "플레이어 없음" 메시지 표시
            // noPlayersMessage.SetActive(true);
            // noPlayersMessage.GetComponent<Text>().text = "플레이어 정보 로딩 중...";

            Debug.Log("[UIManager] Displaying no players message");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] Error displaying no players message: {e.Message}");
        }
    }

    // UI의 현재 현금과 총 가치 업데이트
    public void UpdateCurrentCashandValue()
    {
        if (localPlayerManager == null)
        {
            FindPortfolio();
        }

        if (localPlayerManager != null && localPlayerManager.IsSpawned)
        {
            // 안전한 접근 메서드 사용
            float cash = localPlayerManager.GetPlayerCash();
            float value = localPlayerManager.GetPlayerValue();

            if (currentCashText != null)
            {
                currentCashText.text = cash.ToString("N0", CultureInfo.InvariantCulture);
            }

            if (currentValueText != null)
            {
                currentValueText.text = value.ToString("N0", CultureInfo.InvariantCulture);
            }
        }
        else
        {
            // PlayerManager를 찾을 수 없는 경우 기본값 표시
            if (currentCashText != null)
            {
                currentCashText.text = "0";
            }

            if (currentValueText != null)
            {
                currentValueText.text = "0";
            }
        }
    }

    // 현재 로컬 플레이어의 포트폴리오를 탐색 (수정된 버전)
    public void FindPortfolio()
    {
        // 너무 자주 호출되지 않도록 제한
        if (Time.time - lastFindTime < 2f) return;
        lastFindTime = Time.time;

        Debug.Log("[UIManager] Searching for local PlayerManager...");

        // 방법 1: NetworkRunner를 통해 로컬 플레이어 찾기
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && GameManager.Instance != null)
        {
            PlayerRef localPlayer = runner.LocalPlayer;
            var foundManager = GameManager.Instance.GetPlayerManager(localPlayer);

            if (foundManager != null && foundManager.IsSpawned)
            {
                localPlayerManager = foundManager;
                Debug.Log($"[UIManager] Local PlayerManager found via GameManager for player {localPlayer}!");
                return;
            }
        }

        // 방법 2: FindObjectsOfType으로 모든 PlayerManager 확인 (성능상 최후 수단)
        try
        {
            PlayerManager[] allPlayerManagers = FindObjectsOfType<PlayerManager>();

            foreach (PlayerManager pm in allPlayerManagers)
            {
                if (pm != null && pm.IsSpawned && pm.Object != null)
                {
                    // 로컬 플레이어인지 확인 (Input Authority 체크)
                    if (pm.Object.HasInputAuthority)
                    {
                        localPlayerManager = pm;
                        Debug.Log($"[UIManager] Local PlayerManager found via HasInputAuthority!");
                        return;
                    }

                    // Runner.LocalPlayer와 비교
                    if (runner != null && pm.PlayerRef == runner.LocalPlayer)
                    {
                        localPlayerManager = pm;
                        Debug.Log($"[UIManager] Local PlayerManager found via PlayerRef comparison!");
                        return;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] Error finding PlayerManager: {e.Message}");
        }

        Debug.LogWarning("[UIManager] Local PlayerManager not found.");
    }

    private float lastFindTime = 0f;

    // 로컬 플레이어의 주식 보유량 조회
    public int GetLocalPlayerStockQuantity(string stockName)
    {
        if (localPlayerManager == null)
        {
            FindPortfolio();
        }

        if (localPlayerManager != null && localPlayerManager.IsSpawned)
        {
            return localPlayerManager.GetPlayerStockQuantity(stockName);
        }

        return 0;
    }

    // 플레이어 이름 가져오기 (PlayerInfoManager에서)
    private string GetPlayerDisplayName(PlayerRef playerRef)
    {
        if (PlayerInfoManager.Instance != null)
        {
            var playerInfo = PlayerInfoManager.Instance.GetPlayerInfo(playerRef);
            if (playerInfo.HasValue)
            {
                return playerInfo.Value.nickname.ToString();
            }
        }

        // PlayerInfoManager에서 정보를 못 찾으면 PlayerManager에서 가져오기
        if (GameManager.Instance != null)
        {
            var playerManager = GameManager.Instance.GetPlayerManager(playerRef);
            if (playerManager != null && !string.IsNullOrEmpty(playerManager.NameField))
            {
                return playerManager.NameField;
            }
        }

        return $"Player {playerRef}"; // 기본값
    }

    // ============= 패널 제어 메서드들 =============

    public void InitializeUI()
    {
        ShowGamePanel();
    }

    public void ShowGamePanel()
    {
        if (gamePanel != null) gamePanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (marketPanel2 != null) marketPanel2.SetActive(false);
    }

    public void ShowResultPanel(bool on)
    {
        if (resultPanel != null) resultPanel.SetActive(on);
    }

    public void ShowInventoryPanel()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (marketPanel2 != null) marketPanel2.SetActive(false);
    }

    public void ShowMarketPanel()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(true);
        if (marketPanel2 != null) marketPanel2.SetActive(false);
    }

    public void ShowMarketPanel2(string stockName)
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(true);
        if (marketPanel2 != null) marketPanel2.SetActive(true);
    }

    public void OnCloseButtonClick(GameObject targetGameObject)
    {
        targetGameObject.SetActive(false);
    }
}