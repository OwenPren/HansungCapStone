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
    public GameObject gamePanel; // �⺻ ���� ���� �� ��ư �г�
    public GameObject inventoryPanel; // �κ��丮 �г�
    public GameObject marketPanel; // �ֽ� ���� (���� ���) �г�
    public GameObject marketPanel2; // ���� ���� �� ���� �г�
    public GameObject resultPanel; // ��� ���� �г�

    [Header("Player Stats UI")]
    public TextMeshProUGUI currentCashText; // ���� ������
    public TextMeshProUGUI currentValueText; // ���� �򰡾�

    [Header("Game Info UI")]
    public TextMeshProUGUI currentTimeText; // ���� ���� ī��Ʈ (�߰���)
    public TextMeshProUGUI currentRoundText; // ���� ���� ���� (�߰���)

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

    private PlayerManager localPlayerManager; // ������ �÷��̾� �Ŵ���(��Ʈ�������� �ִ� ��ũ��Ʈ)


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
            currentRoundText.text = GameManager.Instance.CurrentRound.ToString() + "��";
        }

        UpdateCurrentCashandValue();
    }

    //���� ����� ���� ���� ���â ������Ʈ
    public void UpdateResultUI()
    {
        // ��� Ÿ��Ʋ ������Ʈ
        if (currentRoundText != null)
        {
            ResultTitle.text = currentRoundText.text + " ���";
        }
        else
        {
            Debug.LogWarning("currentRoundText�� �Ҵ���� �ʾҽ��ϴ�. ��� Ÿ��Ʋ�� �⺻������ �����մϴ�.");
            ResultTitle.text = "���� ���"; 
        }

        // UI �ؽ�Ʈ ����Ʈ���� ����� �Ҵ�ǰ� ����� ĭ�� �ִ��� Ȯ�� 
        if (currentResultName == null || currentResultValue == null ||
            currentResultName.Count < 4 || currentResultValue.Count < 4)
        {
            Debug.LogError("��� UI �ؽ�Ʈ ����Ʈ�� Inspector�� ����� �Ҵ���� �ʾҰų� ũ�Ⱑ 4 �̸��Դϴ�. ����� ǥ���� �� �����ϴ�.");
            return;
        }

        List<(int Rank, PlayerRef PlayerRef, PlayerManager PlayerManager)> rankedInfo = null;
        if (GameManager.Instance != null)
        {
            rankedInfo = GameManager.Instance.GetRankedPlayersWithInfo(); // GameManager�κ��� Ʃ�� ����Ʈ ������
            if (rankedInfo == null) // GameManager�� ������ �Լ� ��ȯ ���� null�� ��� üũ
            {
                Debug.LogWarning("GameManager.GetRankedPlayersWithInfo() �Լ��� null�� ��ȯ�߽��ϴ�.");
                rankedInfo = new List<(int, PlayerRef, PlayerManager)>(); // �� ����Ʈ�� ó���Ͽ� ������ ���� ����
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance�� null�Դϴ�. ���� �����͸� ������ �� �����ϴ�. UI�� �⺻������ ä��ϴ�.");
            rankedInfo = new List<(int, PlayerRef, PlayerManager)>();
        }

        // �� 4���� �ؽ�Ʈ ������ ��ȸ�ϸ� ������Ʈ
        for (int i = 0; i < 4; i++)
        {
            // ���� ����(i)�� �ش��ϴ� UI �ؽ�Ʈ ��ü �������� (�̸� null üũ)
            TextMeshProUGUI valueTxt = currentResultValue[i];
            TextMeshProUGUI nameTxt = currentResultName[i];
            Image resultImage = currentRankImage[i];

            if (valueTxt == null || nameTxt == null)
            {
                Debug.LogWarning($"��ŷ UI �ؽ�Ʈ ��ü (�ε��� {i}) �� �Ϻΰ� Inspector���� �Ҵ���� �ʾҽ��ϴ�.");
                // �ش� ������ �ǳʶٰ� ���� �ε����� �Ѿ
                continue;
            }

            if (i < rankedInfo.Count)
            {
                // ������ ���Ե� �÷��̾� ������ ä��� 
                var playerRankInfo = rankedInfo[i]; // Ʃ�� ���� ��������

                // PlayerManager ��ü ��������
                PlayerManager player = playerRankInfo.PlayerManager;

                if (player != null) // Ȥ�� PlayerManager ��ü�� null�� ��츦 ��� (���� �幰����)
                {
                    // ���� (���� ����) ǥ��
                    valueTxt.text = player.portfolioReturn.ToString("F2") + "%";
                    nameTxt.text = player.NameField;
                    resultImage.gameObject.SetActive(true);

                }
                else
                {
                    valueTxt.gameObject.SetActive(false);
                    nameTxt.gameObject.SetActive(false);
                    resultImage.gameObject.SetActive(false);
                    Debug.LogWarning($"rankedInfo[{i}]�� PlayerManager ��ü�� null�Դϴ�.");
                }
            }
            else
            {
                // ������ ���Ե��� ���� ������ ĭ�� ���� 
                valueTxt.gameObject.SetActive(false);
                nameTxt.gameObject.SetActive(false);
                resultImage.gameObject.SetActive(false);
            }
        }

        Debug.Log("��ŷ UI ǥ�� ������Ʈ �Ϸ�.");
    }

    // ���� ���۽� ��Ʈâ ������Ʈ
    public void UpdateHintUI(List<string> HintData)
    {
        // currentHintText �迭�� �ʱ�ȭ (���� ��Ʈ �ܿ� ����)
        for (int j = 0; j < currentHintText.Count; j++)
        {
            currentHintText[j].text = "";
        }

        // HintData�� ���̿� currentHintText �迭�� ���� �� �� ���� ����ŭ �ݺ�
        int count = Mathf.Min(HintData.Count, currentHintText.Count);
        for (int i = 0; i < count; i++)
        {
            currentHintText[i].text = $"��Ʈ {i + 1}: {HintData[i]}";
        }

        // ���� HintData�� currentHintText���� ���ٸ� ��� (���� ����)
        //if (HintData.Count > currentHintText.Count)
        //{
        //    Debug.LogWarning($"��� ��Ʈ�� ǥ���� �� �����ϴ�. UI �ؽ�Ʈ ������Ʈ({currentHintText.Count}��)�� ��Ʈ({HintData.Count}��)���� �����ϴ�.");
        //}
    }

    // ���� ����� ���� ��ŷâ ������Ʈ
    public void UpdateCurrentRanking()
    {
        // UI �ؽ�Ʈ ����Ʈ���� ����� �Ҵ�ǰ� �ּ� 4���� ĭ�� �ִ��� Ȯ��
        if (currentRankText == null || currentRankNameText == null || currentRankText.Count < 4 || currentRankNameText.Count < 4)
        {
            Debug.LogError("��ŷ UI �ؽ�Ʈ ����Ʈ�� Inspector�� ����� �Ҵ���� �ʾҰų� ũ�Ⱑ 4 �̸��Դϴ�. ���� ǥ�ø� ������Ʈ�� �� �����ϴ�.");
            return; // ����Ʈ ���°� �ùٸ��� ������ �Լ� ����
        }

        // GameManager �ν��Ͻ� Ȯ�� �� ���� ���� ��������
        List<(int Rank, PlayerRef PlayerRef, PlayerManager PlayerManager)> rankedInfo = null;
        if (GameManager.Instance != null)
        {
            rankedInfo = GameManager.Instance.GetRankedPlayersWithInfo(); // GameManager�κ��� Ʃ�� ����Ʈ ������
            if (rankedInfo == null) // GameManager�� ������ �Լ� ��ȯ ���� null�� ��� üũ
            {
                Debug.LogWarning("GameManager.GetRankedPlayersWithInfo() �Լ��� null�� ��ȯ�߽��ϴ�.");
                rankedInfo = new List<(int, PlayerRef, PlayerManager)>(); // �� ����Ʈ�� ó���Ͽ� ������ ���� ����
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance�� null�Դϴ�. ���� �����͸� ������ �� �����ϴ�. UI�� �⺻������ ä��ϴ�.");
            rankedInfo = new List<(int, PlayerRef, PlayerManager)>(); 
        }

        // �� 4���� �ؽ�Ʈ ������ ��ȸ�ϸ� ������Ʈ
        for (int i = 0; i < 4; i++)
        {
            // ���� ����(i)�� �ش��ϴ� UI �ؽ�Ʈ ��ü �������� (�̸� null üũ)
            TextMeshProUGUI rankTxt = currentRankText[i];
            TextMeshProUGUI nameTxt = currentRankNameText[i];

            if (rankTxt == null || nameTxt == null)
            {
                Debug.LogWarning($"��ŷ UI �ؽ�Ʈ ��ü (�ε��� {i}) �� �Ϻΰ� Inspector���� �Ҵ���� �ʾҽ��ϴ�.");
                // �ش� ������ �ǳʶٰ� ���� �ε����� �Ѿ
                continue;
            }

            // rankedInfo ����Ʈ�� ���� ����(i)�� �ش��ϴ� �÷��̾� ������ �ִ��� Ȯ��
            if (i < rankedInfo.Count)
            {
                // ������ ���Ե� �÷��̾� ������ ä��� 
                var playerRankInfo = rankedInfo[i]; // Ʃ�� ���� ��������

                // PlayerManager ��ü ��������
                PlayerManager player = playerRankInfo.PlayerManager;

                if (player != null) // Ȥ�� PlayerManager ��ü�� null�� ��츦 ��� (���� �幰����)
                {
                    // ���� (���� ����) ǥ��
                    player.ValuationUpdate(player.portfolio);
                    player.UpdatePortfolioReturn();
                    rankTxt.text = playerRankInfo.Rank.ToString();
                    nameTxt.text = player.NameField;
                }
                else
                {
                    rankTxt.text = "-";
                    nameTxt.text = "";
                    Debug.LogWarning($"rankedInfo[{i}]�� PlayerManager ��ü�� null�Դϴ�.");
                }
            }
            else
            {
                // ������ ���Ե��� ���� ������ ĭ�� ���� 
                rankTxt.text = "-"; // ���� ĭ�� "-"
                nameTxt.text = ""; // �̸� ĭ�� ��ĭ
            }
        }

        Debug.Log("��ŷ UI ǥ�� ������Ʈ �Ϸ�.");
    }

    // UI�� ���� �ݾװ� �� �ݾ� ������Ʈ
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

    // ���� ���� �÷��̾��� ��Ʈ������ Ž��
    public void FindPortfolio()
    {
        PlayerManager[] allPlayerManagers = FindObjectsOfType<PlayerManager>(); // ��� PlayerManager ã��

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

    // �г� �ʱ�ȭ
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

    public void ShowResultPanel(bool on)
    {
        if (resultPanel != null) resultPanel.SetActive(on);
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