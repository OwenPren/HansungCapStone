using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField idField;
    public TMP_InputField pwField;
    public TMP_Text debugText;
    public Button loginButton;
    public Button signupButton;
    public Button backButton;

    private string serverBaseUrl = "http://43.203.206.157:3000";

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        signupButton.onClick.AddListener(OnSignupClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    public void OnLoginClicked()
    {
        string id = idField.text.Trim();
        string pw = pwField.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            debugText.text = "아이디와 비밀번호를 모두 입력하세요.";
            return;
        }

        StartCoroutine(LoginRequest(id, pw));
    }

    public void OnSignupClicked()
    {
        SceneManager.LoadScene("SignUpScene");
    }

    public void OnBackClicked()
    {
        SceneManager.LoadScene("LoadingScene");
    }

    IEnumerator LoginRequest(string id, string pw)
    {
        var loginData = new LoginPayload { username = id, password = pw };
        string json = JsonUtility.ToJson(loginData);

        UnityWebRequest req = new UnityWebRequest($"{serverBaseUrl}/login", "POST");
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(jsonBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            // 로그인 성공 → 아이디/닉네임 저장
            PlayerData.instance.userID = id;
            PlayerData.instance.nickname = ExtractNickname(req.downloadHandler.text);

            // Photon CustomProperties에 닉네임 저장
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["nickname"] = PlayerData.instance.nickname;
            Photon.Pun.PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            debugText.text = "로그인 성공!";
            SceneManager.LoadScene("SelectCharacter");
        }
        else
        {
            string errorMsg = req.downloadHandler?.text ?? "오류 발생";
            debugText.text = "로그인 실패: " + errorMsg;
        }
    }
    string ExtractNickname(string json)
    {
        var wrapper = JsonUtility.FromJson<NicknameWrapper>(json);
        return wrapper.nickname;
    }

    [System.Serializable]
    public class LoginPayload
    {
        public string username;
        public string password;
    }
    public class NicknameWrapper
    {
        public string nickname;
    }
}