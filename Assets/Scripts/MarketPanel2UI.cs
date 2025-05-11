// using UnityEngine;
// using UnityEngine.UI;
// using TMPro; 

// public class MarketPanel2UI : MonoBehaviour
// {
//     [Header("UI Elements")]
//     public TextMeshProUGUI stockNameText;
//     public TextMeshProUGUI currentPriceText;
//     public TextMeshProUGUI playerHoldingText;
//     public TMP_InputField quantityInput; // �ż�/�ŵ� ���� �Է� �ʵ�
//     public Button buyButton;
//     public Button sellButton;
//     public Button closeButton; // MarketPanel2�� �ݴ� ��ư

//     private string currentStockName; // ���� ���� �ִ� �ֽ� �̸�

//     void Awake()
//     {
//         if (buyButton != null) buyButton.onClick.AddListener(OnBuyButtonClick);
//         if (sellButton != null) sellButton.onClick.AddListener(OnSellButtonClick);
//         if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClick);

//         // �ʱ⿡�� ��Ȱ��ȭ ������ �� ����
//         gameObject.SetActive(false);
//     }

//     // UIManager�κ��� ȣ��Ǿ� Ư�� ���� ������ �޾� UI�� ǥ��
//     public void DisplayStockInfo(string name)
//     {
//         currentStockName = name;
//         // GameManager�� ���� StockMarketManager�� LocalPlayerManager ����
//         GameManager gm = GameManager.Instance;
//         if (gm == null || gm.stockMarketManager == null || gm.localPlayerManager == null)
//         {
//             gameObject.SetActive(false);
//             return;
//         }

//         // �ֽ� ������ ��������
//         StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
//         if (stock != null)
//         {
//             stockNameText.text = stock.stockName;
//             currentPriceText.text = "���簡: " + stock.currentPrice.ToString("N2"); // �Ҽ��� 2�ڸ����� ǥ��
//         }
//         else
//         {
//             stockNameText.text = "Error";
//             currentPriceText.text = "N/A";
//         }

//         // �÷��̾� ������ �������� (���� �÷��̾� �Ŵ��� ���)
//         int holding = gm.localPlayerManager.GetPlayerStockQuantity(currentStockName);
//         playerHoldingText.text = "������: " + holding.ToString();

//         // �ż�/�ŵ� ���� �Է� �ʵ� �ʱ�ȭ
//         quantityInput.text = "1"; // �⺻ ���� ����
//         // �÷��̾� ���� �� �������� ���� �ż�/�ŵ� ��ư Ȱ��ȭ
//     }

//     // '�ż�' 
//     void OnBuyButtonClick()
//     {
//         if (string.IsNullOrEmpty(currentStockName)) return;

//         // �Էµ� ���� ��������
//         if (int.TryParse(quantityInput.text, out int quantity))
//         {
//             GameManager gm = GameManager.Instance;
//             if (gm != null && gm.stockMarketManager != null && gm.localPlayerManager != null)
//             {
//                 StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
//                 if (stock.currentPrice > 0)
//                 {
//                     // PlayerManager�� �ż� �޼��� ȣ��
//                     bool success = gm.localPlayerManager.BuyStock(currentStockName, quantity, stock.currentPrice);
//                     if (success)
//                     {
//                         Debug.Log("Buy Successful!");
//                         DisplayStockInfo(currentStockName); 
//                     }
//                     else
//                     {
//                         Debug.Log("Buy Failed."); 
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogError($"Could not get current price for {currentStockName}");
//                 }
//             }
//             else
//             {
//                 Debug.LogError("GameManager or Managers not available for Buy operation.");
//             }
//         }
//         else
//         {
//             Debug.Log("Invalid quantity entered.");
//         }
//     }

//     // '�ŵ�' 
//     void OnSellButtonClick()
//     {
//         if (string.IsNullOrEmpty(currentStockName)) return;

//         // �Էµ� ���� ��������
//         if (int.TryParse(quantityInput.text, out int quantity))
//         {
//             GameManager gm = GameManager.Instance;
//             if (gm != null && gm.stockMarketManager != null && gm.localPlayerManager != null)
//             {
//                 StockData stock = gm.stockMarketManager.GetStockData(currentStockName);
//                 if (stock.currentPrice > 0)
//                 {
//                     // PlayerManager�� �ŵ� �޼��� ȣ��
//                     bool success = gm.localPlayerManager.SellStock(currentStockName, quantity, stock.currentPrice);
//                     if (success)
//                     {
//                         Debug.Log("Sell Successful!");
//                         DisplayStockInfo(currentStockName); 
//                     }
//                     else
//                     {
//                         Debug.Log("Sell Failed."); 
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogError($"Could not get current price for {currentStockName}");
//                 }
//             }
//             else
//             {
//                 Debug.LogError("GameManager or Managers not available for Sell operation.");
//             }
//         }
//         else
//         {
//             Debug.Log("Invalid quantity entered.");
//         }
//     }

//     // '�ݱ�' 
//     void OnCloseButtonClick()
//     {
//         // UIManager�� ã�Ƽ� MarketPanel�� ���̰� ��ȯ ��û
//         UIManager uiManager = UIManager.Instance;
//         if (uiManager != null)
//         {
//             uiManager.ShowMarketPanel();
//         }
//         else
//         {
//             Debug.LogError("UIManager instance not found!");
//         }
//     }

//     void OnDestroy()
//     {
//         if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyButtonClick);
//         if (sellButton != null) sellButton.onClick.RemoveListener(OnSellButtonClick);
//         if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseButtonClick);
//     }
// }