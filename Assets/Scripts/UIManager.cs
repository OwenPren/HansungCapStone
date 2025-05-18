using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class UIManager : MonoBehaviour
{

    [Header("UI Panels")]
    public GameObject gamePanel; // �⺻ ���� ���� �� ��ư �г�
    public GameObject inventoryPanel; // �κ��丮 �г�
    public GameObject marketPanel; // �ֽ� ���� (���� ���) �г�
    public GameObject marketPanel2; // ���� ���� �� ���� �г�
    public TextMeshProUGUI currentCashText; // ���� ������
    public TextMeshProUGUI currentValueText; // ���� �򰡾�
    private PlayerManager localPlayerManager; // ������ �÷��̾� �Ŵ���(��Ʈ�������� �ִ� ��ũ��Ʈ)


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
        PlayerManager[] allPlayerManagers = FindObjectsOfType<PlayerManager>(); // �� ��� PlayerManager ã��

        localPlayerManager = allPlayerManagers.FirstOrDefault(pm => pm != null && pm.Object != null && pm.Object.HasInputAuthority); // �� ���� �÷��̾��� �Ŵ��� ���͸�

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
            UpdateCurrentCashandValue(); // ã�ڸ��� UI ������Ʈ
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

    // �⺻ ���� �гθ� ���̰�
    public void ShowGamePanel()
    {
        gamePanel.SetActive(true);
        inventoryPanel.SetActive(false);
        marketPanel.SetActive(false);
        marketPanel2.SetActive(false);
    }

    // �κ��丮 �г��� ���̰�
    public void ShowInventoryPanel()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (marketPanel2 != null) marketPanel2.SetActive(false);
    }

    // �ֽ� ���� (���� ���) �г��� ���̰�
    public void ShowMarketPanel()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(true);
        if (marketPanel2 != null) marketPanel2.SetActive(false);
    }

    // ���� ���� �� ���� �г��� ���̰� �ϰ� ���� ������Ʈ
    public void ShowMarketPanel2(string stockName)
    {
        inventoryPanel.SetActive(false);
        marketPanel.SetActive(true);
        marketPanel2.SetActive(true);
    }
}