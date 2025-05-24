// StockListPanelUI.cs
using UnityEngine;
using UnityEngine.UI; // Button 사용을 위해 필요
using System.Collections.Generic; // List 사용을 위해 필요
using TMPro;

public class StockListPanelUI : MonoBehaviour
{
    [Header("주식 목록 버튼들")]
    // Inspector에서 주식별 버튼들을 여기에 드래그하여 할당합니다.
    public List<Button> stockButtons;
    public List<Button> stockPiece;

    [Header("패널 오브젝트")]
    // 현재 스크립트가 붙어있는 MarketPanel 오브젝트 자체를 할당합니다.
    public GameObject thisPanel;
    // MarketPanel2UI 스크립트가 붙어있는 MarketPanel2 오브젝트를 할당합니다.
    public GameObject stockDetailPanelObject;

    // MarketPanel2UI 컴포넌트 참조 (자주 사용하므로 캐싱)
    private MarketPanel2UI stockDetailPanelUI;

    void Awake()
    {
        // 할당된 stockDetailPanelObject에서 MarketPanel2UI 컴포넌트를 가져옵니다.
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

    // 주식 목록 버튼이 클릭되었을 때 호출되는 메서드
    public void OnStockButtonClick(Button clickedButton)
    {
        // 클릭된 버튼의 Tag 값을 가져옵니다. Tag에 주식 이름이 설정되어 있어야 합니다.
        string stockName = clickedButton.tag;

        TextMeshProUGUI buttonTextComponent = clickedButton.GetComponentInChildren<TextMeshProUGUI>();

        string stockNameKR = buttonTextComponent.text;

        if (string.IsNullOrEmpty(stockName))
        {
            Debug.LogWarning($"Clicked button '{clickedButton.name}' has no Tag (stock name) assigned!");
            return;
        }

        Debug.Log($"'{stockName}' 주식 버튼 클릭됨. 상세 정보 패널 표시 요청.");


        // 상세 정보 패널을 활성화하고 주식 정보를 표시하도록 요청합니다.
        if (stockDetailPanelUI != null)
        {
            stockDetailPanelObject.SetActive(true); // 상세 패널 오브젝트 활성화
            stockDetailPanelUI.DisplayStockInfo(stockName, stockNameKR); // MarketPanel2UI 스크립트의 정보 표시 메서드 호출
        }
        else
        {
            Debug.LogError("Stock Detail Panel UI component is not available.");
        }
    }


    // UIManager 등에서 이 패널을 활성화할 때 호출할 수 있는 메서드
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