using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public TMP_InputField idInput;
    public TMP_InputField passwordInput;
    public TMP_InputField nicknameInput;
    public GameObject changeInfoPanel;
    public TMP_Text resultText;
    public Button editButton;
    public TMP_Text nicknameText;
    public Button applyChangeButton;
    public Button cancelChangeButton;
    public Button IdDuplicatesButton;
    public Button NameDuplicatesButton;

    public AudioClip clickSound;
    private AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (PlayerData.instance != null)
        {
            nicknameText.text = PlayerData.instance.nickname;
        }
        else
            nicknameText.text = "Unknown";
        if (editButton != null)
        {
            editButton.onClick.AddListener(OpenChangeInfoPanel);
        }
        if (applyChangeButton != null)
            applyChangeButton.onClick.AddListener(OnClickApplyChange);
        if (cancelChangeButton != null)
            cancelChangeButton.onClick.AddListener(OnClickCancelChange);
        changeInfoPanel.SetActive(false);
        if (IdDuplicatesButton != null)
            IdDuplicatesButton.onClick.AddListener(OnClickCheckIdDuplicate);
        if (NameDuplicatesButton != null)
            NameDuplicatesButton.onClick.AddListener(OnClickCheckNicknameDuplicate);
        changeInfoPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenChangeInfoPanel()
    {
        changeInfoPanel.SetActive(true);

        idInput.text = PlayerData.instance.userID;
        passwordInput.text = "";    // 보안 문제로 비워둠
        nicknameInput.text = PlayerData.instance.nickname;
    }

    public void OnClickCheckIdDuplicate()
    {
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        StartCoroutine(CheckDuplicate("id", idInput.text.Trim()));
    }

    public void OnClickCheckNicknameDuplicate()
    {
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        StartCoroutine(CheckDuplicate("nickname", nicknameInput.text.Trim()));
    }

    IEnumerator CheckDuplicate(string field, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            resultText.text = "값을 입력해주세요.";
            yield break;
        }

        string url = "";

        if (field == "id")
            url = $"http://43.203.206.157:3000/check-id?username={value}";
        else if (field == "nickname")
            url = $"http://43.203.206.157:3000/check-nick?nickname={value}";

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            // 응답: { available: true } or false
            string json = req.downloadHandler.text;
            var res = JsonUtility.FromJson<CheckResult>(json);
            resultText.text = res.available ? "사용 가능합니다." : "중복된 값입니다.";
        }
        else
        {
            resultText.text = "중복 확인 실패";
        }
    }

    [System.Serializable]
    public class CheckResult
    {
        public bool available;
    }

    // 확인 버튼 클릭 시 서버로 요청하기
    public void OnClickApplyChange()
    {
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        StartCoroutine(UpdateUserInfo());
        changeInfoPanel.SetActive(false);  // 성공 시 패널 닫기
    }
    public void OnClickCancelChange()
    {
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        changeInfoPanel.SetActive(false);
    }

    IEnumerator UpdateUserInfo()
    {
        string url = "http://43.203.206.157:3000/update-user";

        string oldID = PlayerData.instance.userID;
        string newID = string.IsNullOrWhiteSpace(idInput.text) ? oldID : idInput.text.Trim();
        string newPassword = string.IsNullOrWhiteSpace(passwordInput.text) ? null : passwordInput.text.Trim();
        string newNickname = string.IsNullOrWhiteSpace(nicknameInput.text) ? null : nicknameInput.text.Trim();

        // 직접 JSON 문자열 만들기
        string json = "{";
        json += $"\"oldID\":\"{oldID}\",";
        json += $"\"newID\":\"{newID}\"";

        if (!string.IsNullOrEmpty(newPassword))
            json += $",\"newPassword\":\"{newPassword}\"";

        if (!string.IsNullOrEmpty(newNickname))
            json += $",\"newNickname\":\"{newNickname}\"";

        json += "}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(jsonBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("회원정보 수정 실패: " + req.error);
            resultText.text = "수정 실패: " + req.downloadHandler.text;
        }
        else
        {
            Debug.Log("회원정보 수정 성공");

            PlayerData.instance.userID = newID;
            if (!string.IsNullOrEmpty(newNickname))
                PlayerData.instance.nickname = newNickname;

            nicknameText.text = PlayerData.instance.nickname;
            resultText.text = "회원정보 수정 완료";
            changeInfoPanel.SetActive(false);

            if (newID != oldID)
            {
                // 소리 끝나기 전에 씬이 바뀌지 않도록 딜레이
                StartCoroutine(DelayedSceneLoad());
            }
        }
    }
    IEnumerator DelayedSceneLoad()
    {
        yield return new WaitForSeconds(0.3f); // 사운드 재생 시간에 따라 조정
        SceneManager.LoadScene("LogInScene");
    }
}
