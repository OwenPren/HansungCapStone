using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Text;
using UnityEngine.SceneManagement;

public class SignupManager : MonoBehaviour
{
    public TMP_InputField idField;
    public TMP_InputField pwField;
    public TMP_InputField nickField;
    public TMP_Text debugText;

    public Button signupButton;
    public Button checkIdButton;
    public Button checkNickButton;

    private string serverBaseUrl = "http://43.203.206.157:3000"; // ← EC2 IP로 수정

    private bool isIdChecked = false;
    private bool isNickChecked = false;

    void Start()
    {
        signupButton.onClick.AddListener(OnSignupButtonClicked);
        checkIdButton.onClick.AddListener(OnCheckIdClicked);
        checkNickButton.onClick.AddListener(OnCheckNickClicked);
    }

    public void OnSignupButtonClicked()
    {
        string id = idField.text.Trim();
        string pw = pwField.text.Trim();
        string nick = nickField.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw) || string.IsNullOrEmpty(nick))
        {
            debugText.text = "모든 필드를 입력하세요.";
            return;
        }

        if (!isIdChecked || !isNickChecked)
        {
            debugText.text = "중복 확인을 완료하세요.";
            return;
        }

        StartCoroutine(RegisterUser(id, pw, nick));
    }

    IEnumerator RegisterUser(string id, string pw, string nick)
    {
        var userData = new SignupData { username = id, password = pw, nickname = nick };
        string json = JsonUtility.ToJson(userData);

        UnityWebRequest req = new UnityWebRequest($"{serverBaseUrl}/signup", "POST");
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(jsonBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            debugText.text = "회원가입 성공!";
            SceneManager.LoadScene("LogInScene");
        }
        else
        {
            debugText.text = $"회원가입 실패: {req.downloadHandler.text}";
        }
    }

    public void OnCheckIdClicked()
    {
        string id = idField.text.Trim();
        if (string.IsNullOrEmpty(id))
        {
            debugText.text = "아이디를 입력하세요.";
            return;
        }
        StartCoroutine(CheckId(id));
    }

    IEnumerator CheckId(string id)
    {
        UnityWebRequest req = UnityWebRequest.Get($"{serverBaseUrl}/check-id?username={id}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler?.text;
            if (!string.IsNullOrEmpty(response) && response.Contains("\"available\":true"))
            {
                debugText.text = "사용 가능한 아이디입니다.";
                isIdChecked = true;
            }
            else
            {
                debugText.text = "이미 사용 중인 아이디입니다.";
                isIdChecked = false;
            }
        }
        else
        {
            string errorMsg = req.error ?? "서버 응답 없음";
            debugText.text = "아이디 확인 실패: " + errorMsg;
            isIdChecked = false;
        }
    }

    public void OnCheckNickClicked()
    {
        string nick = nickField.text.Trim();
        if (string.IsNullOrEmpty(nick))
        {
            debugText.text = "닉네임을 입력하세요.";
            return;
        }
        StartCoroutine(CheckNick(nick));
    }

    IEnumerator CheckNick(string nick)
    {
        UnityWebRequest req = UnityWebRequest.Get($"{serverBaseUrl}/check-nick?nickname={nick}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler?.text;
            if (!string.IsNullOrEmpty(response) && response.Contains("\"available\":true"))
            {
                debugText.text = "사용 가능한 닉네임입니다.";
                isNickChecked = true;
            }
            else
            {
                debugText.text = "이미 사용 중인 닉네임입니다.";
                isNickChecked = false;
            }
        }
        else
        {
            string errorMsg = req.error ?? "서버 응답 없음";
            debugText.text = "닉네임 확인 실패: " + errorMsg;
            isNickChecked = false;
        }
    }

    [System.Serializable]
    public class SignupData
    {
        public string username;
        public string password;
        public string nickname;
    }
}