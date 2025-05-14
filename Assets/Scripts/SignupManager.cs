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

    private string serverBaseUrl = "http://43.203.206.157:3000"; // �� EC2 IP�� ����

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
            debugText.text = "��� �ʵ带 �Է��ϼ���.";
            return;
        }

        if (!isIdChecked || !isNickChecked)
        {
            debugText.text = "�ߺ� Ȯ���� �Ϸ��ϼ���.";
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
            debugText.text = "ȸ������ ����!";
            SceneManager.LoadScene("LogInScene");
        }
        else
        {
            debugText.text = $"ȸ������ ����: {req.downloadHandler.text}";
        }
    }

    public void OnCheckIdClicked()
    {
        string id = idField.text.Trim();
        if (string.IsNullOrEmpty(id))
        {
            debugText.text = "���̵� �Է��ϼ���.";
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
                debugText.text = "��� ������ ���̵��Դϴ�.";
                isIdChecked = true;
            }
            else
            {
                debugText.text = "�̹� ��� ���� ���̵��Դϴ�.";
                isIdChecked = false;
            }
        }
        else
        {
            string errorMsg = req.error ?? "���� ���� ����";
            debugText.text = "���̵� Ȯ�� ����: " + errorMsg;
            isIdChecked = false;
        }
    }

    public void OnCheckNickClicked()
    {
        string nick = nickField.text.Trim();
        if (string.IsNullOrEmpty(nick))
        {
            debugText.text = "�г����� �Է��ϼ���.";
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
                debugText.text = "��� ������ �г����Դϴ�.";
                isNickChecked = true;
            }
            else
            {
                debugText.text = "�̹� ��� ���� �г����Դϴ�.";
                isNickChecked = false;
            }
        }
        else
        {
            string errorMsg = req.error ?? "���� ���� ����";
            debugText.text = "�г��� Ȯ�� ����: " + errorMsg;
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