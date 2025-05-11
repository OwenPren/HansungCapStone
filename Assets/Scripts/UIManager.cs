using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // �̱��� �ν��Ͻ�
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject gamePanel; // �⺻ ���� ���� �� ��ư �г�
    public GameObject inventoryPanel; // �κ��丮 �г�
    public GameObject marketPanel; // �ֽ� ���� (���� ���) �г�
    public GameObject marketPanel2; // ���� ���� �� ���� �г�

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // ���� ������ �� ������Ʈ�� �ı�
        }
        else 
        {
            Instance = this; // �� ������Ʈ�� �̱��� �ν��Ͻ��� ����
            DontDestroyOnLoad(gameObject);
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