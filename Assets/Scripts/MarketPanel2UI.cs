using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion; // Fusion 네임스페이스 추가
using System.Linq; // LINQ를 사용하기 위해 추가

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
    //public UIManager UIManager;

    private string currentStockName;

    // 로컬 플레이어의 PlayerManager 참조
    private PlayerManager localPlayerManager;

    // MarketPanel2UI가 활성화될 때마다 로컬 플레이어의 PlayerManager를 찾습니다.
    void OnEnable()
    {
        FindLocalPlayerManager();
    }

    void FindLocalPlayerManager()
    {
        // 씬에 있는 모든 PlayerManager 컴포넌트를 찾습니다.
        PlayerManager[] allPlayerManagers = FindObjectsOfType<PlayerManager>();

        // 그 중에서 현재 클라이언트의 입력 권한을 가진 PlayerManager를 찾습니다.
        // NetworkBehaviour의 Object.HasInputAuthority를 사용합니다.
        localPlayerManager = allPlayerManagers.FirstOrDefault(pm => pm.Object != null && pm.Object.HasInputAuthority);

        if (localPlayerManager != null)
        {
            Debug.Log("Local PlayerManager found!");
        }
        else
        {
            Debug.LogWarning("Local PlayerManager not found.");
        }
    }


    public void DisplayStockInfo(string name, string nameKR)
    {
        gameObject.SetActive(true);
        currentStockName = name;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.stockMarketManager == null)
        {
            gameObject.SetActive(false);
            return;
        }

        StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
        if (stock != null)
        {
            stockNameText.text = nameKR;
            currentPriceText.text = "현재가: " + stock.currentPrice.ToString("N2");

            // 로컬 플레이어의 보유량 표시
            if (localPlayerManager != null)
            {
                int holding = localPlayerManager.GetPlayerStockQuantity(currentStockName);
                playerHoldingText.text = "보유 현황: " + holding.ToString()+" 개 보유중";
            }
            else
            {
                playerHoldingText.text = "보유량: N/A (플레이어 정보 없음)";
            }
        }
        else
        {
            stockNameText.text = "Error";
            currentPriceText.text = "N/A";
            playerHoldingText.text = "보유량: N/A";
        }

        quantityInput.text = "1";

    }

    public void OnIncrementButtonClick()
    {
        if (quantityInput == null) return;

        if (int.TryParse(quantityInput.text, out int currentQuantity))
        {
            // Increment quantity
            currentQuantity++;
            quantityInput.text = currentQuantity.ToString();
            Debug.Log("Quantity incremented to: " + currentQuantity);
        }
        else
        {
            // If parsing fails, set to default (1)
            quantityInput.text = "1";
            Debug.LogWarning("Invalid quantity input, setting to 1.");
        }
    }

    // Decrement button click handler
    public void OnDecrementButtonClick()
    {
        if (quantityInput == null) return;

        if (int.TryParse(quantityInput.text, out int currentQuantity))
        {
            // Decrement quantity, but not below 1
            currentQuantity = Mathf.Max(1, currentQuantity - 1);
            quantityInput.text = currentQuantity.ToString();
            Debug.Log("Quantity decremented to: " + currentQuantity);
        }
        else
        {
            // If parsing fails, set to default (1)
            quantityInput.text = "1";
            Debug.LogWarning("Invalid quantity input, setting to 1.");
        }
    }

    public void OnBuyButtonClick()
    {
        if (string.IsNullOrEmpty(currentStockName)) return;
        if (localPlayerManager == null)
        {
            Debug.LogError("Local PlayerManager not available for Buy operation.");
            return;
        }

        if (int.TryParse(quantityInput.text, out int quantity))
        {
            GameManager.Instance.HandleBuyRequest(localPlayerManager.Object.InputAuthority, currentStockName, quantity);
        }

        if (localPlayerManager != null)
        {
            int holding = localPlayerManager.GetPlayerStockQuantity(currentStockName);
            playerHoldingText.text = "보유 현황: " + holding.ToString() + " 개 보유중";
        }

        else
        {
            Debug.Log("Invalid quantity entered.");
        }
    }


    public void OnSellButtonClick()
    {
        if (string.IsNullOrEmpty(currentStockName)) return;
        if (localPlayerManager == null)
        {
            Debug.LogError("Local PlayerManager not available for Sell operation.");
            return;
        }


        if (int.TryParse(quantityInput.text, out int quantity))
        {
            GameManager.Instance.HandleSellRequest(localPlayerManager.Object.InputAuthority, currentStockName, quantity);
        }

        if (localPlayerManager != null)
        {
            int holding = localPlayerManager.GetPlayerStockQuantity(currentStockName);
            playerHoldingText.text = "보유 현황: " + holding.ToString()+" 개 보유중";
        }
        else
        {
            Debug.Log("Invalid quantity entered.");
        }
    }

    public void OnCloseButtonClick()
    {
        //UIManager uiManager = UIManager.Instance; // UIManager.Instance는 GameManager처럼 싱글톤으로 가정합니다.
        //if (uiManager != null)
        //{
        //    gameObject.SetActive(false); // 일단 이 패널만 비활성화
        //    uiManager.ShowMarketPanel(); // UIManager에 MarketPanel (목록)을 보여주는 메서드가 있다고 가정
        //}
        //else
        //{
            //Debug.LogError("UIManager instance not found!");
       gameObject.SetActive(false); // UIManager 없으면 일단 패널 닫기
        //}
    }

    void OnDestroy()
    {
        if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyButtonClick);
        if (sellButton != null) sellButton.onClick.RemoveListener(OnSellButtonClick);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseButtonClick);
    }
}