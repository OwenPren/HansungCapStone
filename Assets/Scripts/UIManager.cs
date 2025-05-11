using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject gamePanel; // 기본 게임 정보 및 버튼 패널
    public GameObject inventoryPanel; // 인벤토리 패널
    public GameObject marketPanel; // 주식 시장 (종목 목록) 패널
    public GameObject marketPanel2; // 개별 종목 상세 정보 패널

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 새로 생성된 이 오브젝트를 파괴
        }
        else 
        {
            Instance = this; // 이 오브젝트를 싱글톤 인스턴스로 설정
            DontDestroyOnLoad(gameObject);
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