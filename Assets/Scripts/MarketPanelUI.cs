// StockListPanelUI.cs
using UnityEngine;
using UnityEngine.UI; // Button ����� ���� �ʿ�
using System.Collections.Generic; // List ����� ���� �ʿ�
using TMPro;

public class StockListPanelUI : MonoBehaviour
{
    [Header("�ֽ� ��� ��ư��")]
    // Inspector���� �ֽĺ� ��ư���� ���⿡ �巡���Ͽ� �Ҵ��մϴ�.
    public List<Button> stockButtons;
    public List<Button> stockPiece;

    [Header("�г� ������Ʈ")]
    // ���� ��ũ��Ʈ�� �پ��ִ� MarketPanel ������Ʈ ��ü�� �Ҵ��մϴ�.
    public GameObject thisPanel;
    // MarketPanel2UI ��ũ��Ʈ�� �پ��ִ� MarketPanel2 ������Ʈ�� �Ҵ��մϴ�.
    public GameObject stockDetailPanelObject;

    // MarketPanel2UI ������Ʈ ���� (���� ����ϹǷ� ĳ��)
    private MarketPanel2UI stockDetailPanelUI;

    void Awake()
    {
        // �Ҵ�� stockDetailPanelObject���� MarketPanel2UI ������Ʈ�� �����ɴϴ�.
        if (stockDetailPanelObject != null)
        {
            stockDetailPanelUI = stockDetailPanelObject.GetComponent<MarketPanel2UI>();
            if (stockDetailPanelUI == null)
            {
                Debug.LogError("Stock Detail Panel Object does not have a MarketPanel2UI component assigned!", stockDetailPanelObject);
            }
        }
        else
        {
            Debug.LogError("Stock Detail Panel Object is not assigned in StockListPanelUI!", this);
        }
    }

    // �ֽ� ��� ��ư�� Ŭ���Ǿ��� �� ȣ��Ǵ� �޼���
    public void OnStockButtonClick(Button clickedButton)
    {
        // Ŭ���� ��ư�� Tag ���� �����ɴϴ�. Tag�� �ֽ� �̸��� �����Ǿ� �־�� �մϴ�.
        string stockName = clickedButton.tag;

        TextMeshProUGUI buttonTextComponent = clickedButton.GetComponentInChildren<TextMeshProUGUI>();

        string stockNameKR = buttonTextComponent.text;

        if (string.IsNullOrEmpty(stockName))
        {
            Debug.LogWarning($"Clicked button '{clickedButton.name}' has no Tag (stock name) assigned!");
            return;
        }

        Debug.Log($"'{stockName}' �ֽ� ��ư Ŭ����. �� ���� �г� ǥ�� ��û.");


        // �� ���� �г��� Ȱ��ȭ�ϰ� �ֽ� ������ ǥ���ϵ��� ��û�մϴ�.
        if (stockDetailPanelUI != null)
        {
            stockDetailPanelObject.SetActive(true); // �� �г� ������Ʈ Ȱ��ȭ
            stockDetailPanelUI.DisplayStockInfo(stockName, stockNameKR); // MarketPanel2UI ��ũ��Ʈ�� ���� ǥ�� �޼��� ȣ��
        }
        else
        {
            Debug.LogError("Stock Detail Panel UI component is not available.");
        }
    }


    // UIManager ��� �� �г��� Ȱ��ȭ�� �� ȣ���� �� �ִ� �޼���
    public void ShowPanel()
    {
        if (thisPanel != null)
        {
            thisPanel.SetActive(true);
            if (stockDetailPanelObject != null)
            {
                stockDetailPanelObject.SetActive(false);
            }
        }
    }
}