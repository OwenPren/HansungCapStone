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
    public GameObject gamePanel; // 기본 게임 정보 및 버튼 패널
    public GameObject inventoryPanel; // 인벤토리 패널
    public GameObject marketPanel; // 주식 시장 (종목 목록) 패널
    public GameObject marketPanel2; // 개별 종목 상세 정보 패널
    public GameObject resultPanel; // 결과 정보 패널

    [Header("Player Stats UI")]
    public TextMeshProUGUI currentCashText; // 현재 보유액
    public TextMeshProUGUI currentValueText; // 현재 평가액

    [Header("Game Info UI")]
    public TextMeshProUGUI currentTimeText; // 현재 남은 카운트 (추가됨)
    public TextMeshProUGUI currentRoundText; // 현재 남은 라운드 (추가됨)

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

    private PlayerManager localPlayerManager; // 본인의 플레이어 매니저(포트폴리오가 있는 스크립트)


    void Update()
    {
        if (GameManager.Instance != null && currentTimeText != null && GameManager.Instance.State == GameState.Ended)
        {
            int remainingTime = (int)GameManager.Instance.waitTimer;
            currentTimeText.text = remainingTime.ToString();
        }
        else 
        {
            int remainingTime = (int)GameManager.Instance.Timer;
            currentTimeText.text = remainingTime.ToString();
        }

        if (GameManager.Instance != null && currentRoundText != null)
        {
            currentRoundText.text = GameManager.Instance.CurrentRound.ToString() + "월";
        }

        UpdateCurrentCashandValue();
    }

    //라운드 종료시 현재 라운드 결과창 업데이트
    public void UpdateResultUI()
    {
        // 결과 타이틀 업데이트
        if (currentRoundText != null)
        {
            ResultTitle.text = currentRoundText.text + " 결과";
        }
        else
        {
            Debug.LogWarning("currentRoundText가 할당되지 않았습니다. 결과 타이틀을 기본값으로 설정합니다.");
            ResultTitle.text = "최종 결과"; 
        }

        // UI 텍스트 리스트들이 제대로 할당되고 충분한 칸이 있는지 확인 
        if (currentResultName == null || currentResultValue == null ||
            currentResultName.Count < 4 || currentResultValue.Count < 4)
        {
            Debug.LogError("결과 UI 텍스트 리스트가 Inspector에 제대로 할당되지 않았거나 크기가 4 미만입니다. 결과를 표시할 수 없습니다.");
            return;
        }

        List<(int Rank, PlayerRef PlayerRef, PlayerManager PlayerManager)> rankedInfo = null;
        if (GameManager.Instance != null)
        {
            rankedInfo = GameManager.Instance.GetRankedPlayersWithInfo(); // GameManager로부터 튜플 리스트 가져옴
            if (rankedInfo == null) // GameManager는 있으나 함수 반환 값이 null인 경우 체크
            {
                Debug.LogWarning("GameManager.GetRankedPlayersWithInfo() 함수가 null을 반환했습니다.");
                rankedInfo = new List<(int, PlayerRef, PlayerManager)>(); // 빈 리스트로 처리하여 나머지 로직 수행
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance가 null입니다. 순위 데이터를 가져올 수 없습니다. UI를 기본값으로 채웁니다.");
            rankedInfo = new List<(int, PlayerRef, PlayerManager)>();
        }

        // 총 4개의 텍스트 슬롯을 순회하며 업데이트
        for (int i = 0; i < 4; i++)
        {
            // 현재 순번(i)에 해당하는 UI 텍스트 객체 가져오기 (미리 null 체크)
            TextMeshProUGUI valueTxt = currentResultValue[i];
            TextMeshProUGUI nameTxt = currentResultName[i];
            Image resultImage = currentRankImage[i];

            if (valueTxt == null || nameTxt == null)
            {
                Debug.LogWarning($"랭킹 UI 텍스트 객체 (인덱스 {i}) 중 일부가 Inspector에서 할당되지 않았습니다.");
                // 해당 슬롯은 건너뛰고 다음 인덱스로 넘어감
                continue;
            }

            if (i < rankedInfo.Count)
            {
                // 순위에 포함된 플레이어 정보로 채우기 
                var playerRankInfo = rankedInfo[i]; // 튜플 정보 가져오기

                // PlayerManager 객체 가져오기
                PlayerManager player = playerRankInfo.PlayerManager;

                if (player != null) // 혹시 PlayerManager 객체가 null일 경우를 대비 (극히 드물지만)
                {
                    // 순위 (단일 숫자) 표시
                    valueTxt.text = player.portfolioReturn.ToString("F2") + "%";
                    nameTxt.text = player.NameField;
                    resultImage.gameObject.SetActive(true);

                }
                else
                {
                    valueTxt.gameObject.SetActive(false);
                    nameTxt.gameObject.SetActive(false);
                    resultImage.gameObject.SetActive(false);
                    Debug.LogWarning($"rankedInfo[{i}]의 PlayerManager 객체가 null입니다.");
                }
            }
            else
            {
                // 순위에 포함되지 않은 나머지 칸을 비우기 
                valueTxt.gameObject.SetActive(false);
                nameTxt.gameObject.SetActive(false);
                resultImage.gameObject.SetActive(false);
            }
        }

        Debug.Log("랭킹 UI 표시 업데이트 완료.");
    }

    // 라운드 시작시 힌트창 업데이트
    public void UpdateHintUI(List<string> HintData)
    {
        // currentHintText 배열을 초기화 (이전 힌트 잔여 방지)
        for (int j = 0; j < currentHintText.Count; j++)
        {
            currentHintText[j].text = "";
        }

        // HintData의 길이와 currentHintText 배열의 길이 중 더 작은 값만큼 반복
        int count = Mathf.Min(HintData.Count, currentHintText.Count);
        for (int i = 0; i < count; i++)
        {
            currentHintText[i].text = $"힌트 {i + 1}: {HintData[i]}";
        }

        // 만약 HintData가 currentHintText보다 많다면 경고 (선택 사항)
        //if (HintData.Count > currentHintText.Count)
        //{
        //    Debug.LogWarning($"모든 힌트를 표시할 수 없습니다. UI 텍스트 컴포넌트({currentHintText.Count}개)가 힌트({HintData.Count}개)보다 적습니다.");
        //}
    }

    // 라운드 종료시 현재 랭킹창 업데이트
    public void UpdateCurrentRanking()
    {
        // UI 텍스트 리스트들이 제대로 할당되고 최소 4개의 칸이 있는지 확인
        if (currentRankText == null || currentRankNameText == null || currentRankText.Count < 4 || currentRankNameText.Count < 4)
        {
            Debug.LogError("랭킹 UI 텍스트 리스트가 Inspector에 제대로 할당되지 않았거나 크기가 4 미만입니다. 순위 표시를 업데이트할 수 없습니다.");
            return; // 리스트 상태가 올바르지 않으면 함수 종료
        }

        // GameManager 인스턴스 확인 및 순위 정보 가져오기
        List<(int Rank, PlayerRef PlayerRef, PlayerManager PlayerManager)> rankedInfo = null;
        if (GameManager.Instance != null)
        {
            rankedInfo = GameManager.Instance.GetRankedPlayersWithInfo(); // GameManager로부터 튜플 리스트 가져옴
            if (rankedInfo == null) // GameManager는 있으나 함수 반환 값이 null인 경우 체크
            {
                Debug.LogWarning("GameManager.GetRankedPlayersWithInfo() 함수가 null을 반환했습니다.");
                rankedInfo = new List<(int, PlayerRef, PlayerManager)>(); // 빈 리스트로 처리하여 나머지 로직 수행
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance가 null입니다. 순위 데이터를 가져올 수 없습니다. UI를 기본값으로 채웁니다.");
            rankedInfo = new List<(int, PlayerRef, PlayerManager)>(); 
        }

        // 총 4개의 텍스트 슬롯을 순회하며 업데이트
        for (int i = 0; i < 4; i++)
        {
            // 현재 순번(i)에 해당하는 UI 텍스트 객체 가져오기 (미리 null 체크)
            TextMeshProUGUI rankTxt = currentRankText[i];
            TextMeshProUGUI nameTxt = currentRankNameText[i];

            if (rankTxt == null || nameTxt == null)
            {
                Debug.LogWarning($"랭킹 UI 텍스트 객체 (인덱스 {i}) 중 일부가 Inspector에서 할당되지 않았습니다.");
                // 해당 슬롯은 건너뛰고 다음 인덱스로 넘어감
                continue;
            }

            // rankedInfo 리스트에 현재 순번(i)에 해당하는 플레이어 정보가 있는지 확인
            if (i < rankedInfo.Count)
            {
                // 순위에 포함된 플레이어 정보로 채우기 
                var playerRankInfo = rankedInfo[i]; // 튜플 정보 가져오기

                // PlayerManager 객체 가져오기
                PlayerManager player = playerRankInfo.PlayerManager;

                if (player != null) // 혹시 PlayerManager 객체가 null일 경우를 대비 (극히 드물지만)
                {
                    // 순위 (단일 숫자) 표시
                    player.ValuationUpdate(player.portfolio);
                    player.UpdatePortfolioReturn();
                    rankTxt.text = playerRankInfo.Rank.ToString();
                    nameTxt.text = player.NameField;
                }
                else
                {
                    rankTxt.text = "-";
                    nameTxt.text = "";
                    Debug.LogWarning($"rankedInfo[{i}]의 PlayerManager 객체가 null입니다.");
                }
            }
            else
            {
                // 순위에 포함되지 않은 나머지 칸을 비우기 
                rankTxt.text = "-"; // 순위 칸은 "-"
                nameTxt.text = ""; // 이름 칸은 빈칸
            }
        }

        Debug.Log("랭킹 UI 표시 업데이트 완료.");
    }

    // UI의 보유 금액과 평가 금액 업데이트
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

    // 현재 로컬 플레이어의 포트폴리오 탐색
    public void FindPortfolio()
    {
        PlayerManager[] allPlayerManagers = FindObjectsOfType<PlayerManager>(); // 모든 PlayerManager 찾기

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

    // 패널 초기화
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

    public void ShowResultPanel(bool on)
    {
        if (resultPanel != null) resultPanel.SetActive(on);
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