using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class MarketPanel2UI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI stockNameText;
    public TextMeshProUGUI currentPriceText;
    public TextMeshProUGUI playerHoldingText;
    public TMP_InputField quantityInput; // 매수/매도 수량 입력 필드
    public Button buyButton;
    public Button sellButton;
    public Button closeButton; // MarketPanel2를 닫는 버튼

    private string currentStockName; // 현재 보고 있는 주식 이름

    void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyButtonClick);
        if (sellButton != null) sellButton.onClick.AddListener(OnSellButtonClick);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClick);

        // 초기에는 비활성화 상태일 수 있음
        gameObject.SetActive(false);
    }

    // UIManager로부터 호출되어 특정 종목 정보를 받아 UI에 표시
    public void DisplayStockInfo(string name)
    {
        currentStockName = name;
        // GameManager를 통해 StockMarketManager와 LocalPlayerManager 접근
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.stockMarketManager == null || gm.localPlayerManager == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 주식 데이터 가져오기
        StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
        if (stock != null)
        {
            stockNameText.text = stock.stockName;
            currentPriceText.text = "현재가: " + stock.currentPrice.ToString("N2"); // 소수점 2자리까지 표시
        }
        else
        {
            stockNameText.text = "Error";
            currentPriceText.text = "N/A";
        }

        // 플레이어 보유량 가져오기 (로컬 플레이어 매니저 사용)
        int holding = gm.localPlayerManager.GetPlayerStockQuantity(currentStockName);
        playerHoldingText.text = "보유량: " + holding.ToString();

        // 매수/매도 수량 입력 필드 초기화
        quantityInput.text = "1"; // 기본 수량 설정
        // 플레이어 현금 및 보유량에 따라 매수/매도 버튼 활성화
    }

    // '매수' 
    void OnBuyButtonClick()
    {
        if (string.IsNullOrEmpty(currentStockName)) return;

        // 입력된 수량 가져오기
        if (int.TryParse(quantityInput.text, out int quantity))
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.stockMarketManager != null && gm.localPlayerManager != null)
            {
                StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
                if (stock.currentPrice > 0)
                {
                    // PlayerManager의 매수 메서드 호출
                    bool success = gm.localPlayerManager.BuyStock(currentStockName, quantity, stock.currentPrice);
                    if (success)
                    {
                        Debug.Log("Buy Successful!");
                        DisplayStockInfo(currentStockName); 
                    }
                    else
                    {
                        Debug.Log("Buy Failed."); 
                    }
                }
                else
                {
                    Debug.LogError($"Could not get current price for {currentStockName}");
                }
            }
            else
            {
                Debug.LogError("GameManager or Managers not available for Buy operation.");
            }
        }
        else
        {
            Debug.Log("Invalid quantity entered.");
        }
    }

    // '매도' 
    void OnSellButtonClick()
    {
        if (string.IsNullOrEmpty(currentStockName)) return;

        // 입력된 수량 가져오기
        if (int.TryParse(quantityInput.text, out int quantity))
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.stockMarketManager != null && gm.localPlayerManager != null)
            {
                StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
                if (stock.currentPrice > 0)
                {
                    // PlayerManager의 매도 메서드 호출
                    bool success = gm.localPlayerManager.SellStock(currentStockName, quantity, stock.currentPrice);
                    if (success)
                    {
                        Debug.Log("Sell Successful!");
                        DisplayStockInfo(currentStockName); 
                    }
                    else
                    {
                        Debug.Log("Sell Failed."); 
                    }
                }
                else
                {
                    Debug.LogError($"Could not get current price for {currentStockName}");
                }
            }
            else
            {
                Debug.LogError("GameManager or Managers not available for Sell operation.");
            }
        }
        else
        {
            Debug.Log("Invalid quantity entered.");
        }
    }

    // '닫기' 
    void OnCloseButtonClick()
    {
        // UIManager를 찾아서 MarketPanel만 보이게 전환 요청
        UIManager uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.ShowMarketPanel();
        }
        else
        {
            Debug.LogError("UIManager instance not found!");
        }
    }

    void OnDestroy()
    {
        if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyButtonClick);
        if (sellButton != null) sellButton.onClick.RemoveListener(OnSellButtonClick);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseButtonClick);
    }
}