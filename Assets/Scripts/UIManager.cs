using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class UIManager : MonoBehaviour
{

    [Header("UI Panels")]
    public GameObject gamePanel; // 기본 게임 정보 및 버튼 패널
    public GameObject inventoryPanel; // 인벤토리 패널
    public GameObject marketPanel; // 주식 시장 (종목 목록) 패널
    public GameObject marketPanel2; // 개별 종목 상세 정보 패널
    public TextMeshProUGUI currentCashText; // 현재 보유액
    public TextMeshProUGUI currentValueText; // 현재 평가액
    private PlayerManager localPlayerManager; // 본인의 플레이어 매니저(포트폴리오가 있는 스크립트)


    public void UpdateCurrentCashandValue()
    {
        if (localPlayerManager == null)
        {
            FindPortfolio();
        }
       
        if (localPlayerManager != null)
        {
            currentCashText.text = localPlayerManager.playerCash.ToString("N0", CultureInfo.InvariantCulture);
            currentValueText.text = localPlayerManager.playerValue.ToString("N0", CultureInfo.InvariantCulture);
        }
    }

    public void FindPortfolio()
    {
        PlayerManager[] allPlayerManagers = FindObjectsOfType<PlayerManager>(); // ① 모든 PlayerManager 찾기

        localPlayerManager = allPlayerManagers.FirstOrDefault(pm => pm != null && pm.Object != null && pm.Object.HasInputAuthority); // ② 로컬 플레이어의 매니저 필터링

        if (localPlayerManager != null)
        {
            Debug.Log("Local PlayerManager found!");
        }
        else
        {
            Debug.LogWarning("Local PlayerManager not found.");
        }

        if (localPlayerManager != null)
        {
            UpdateCurrentCashandValue(); // 찾자마자 UI 업데이트
        }
        else
        {
            Debug.LogError("Cannot access portfolio or update UI, localPlayerManager is null.");
        }
    }

    public void InitializeUI()
    {
        ShowGamePanel();
    }

    // 기본 게임 패널만 보이게
    public void ShowGamePanel()
    {
        gamePanel.SetActive(true);
        inventoryPanel.SetActive(false);
        marketPanel.SetActive(false);
        marketPanel2.SetActive(false);
    }

    // 인벤토리 패널을 보이게
    public void ShowInventoryPanel()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (marketPanel2 != null) marketPanel2.SetActive(false);
    }

    // 주식 시장 (종목 목록) 패널을 보이게
    public void ShowMarketPanel()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(true);
        if (marketPanel2 != null) marketPanel2.SetActive(false);
    }

    // 개별 종목 상세 정보 패널을 보이게 하고 정보 업데이트
    public void ShowMarketPanel2(string stockName)
    {
        inventoryPanel.SetActive(false);
        marketPanel.SetActive(true);
        marketPanel2.SetActive(true);
    }
}