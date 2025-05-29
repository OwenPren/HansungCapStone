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

    public AudioClip clickSound;
    private AudioSource audioSource;

    private string serverBaseUrl = "http://43.203.206.157:3000";

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        loginButton.onClick.AddListener(OnLoginClicked);
        signupButton.onClick.AddListener(OnSignupClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    public void OnLoginClicked()
    {
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
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
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // 소리 끝나기 전에 씬이 바뀌지 않도록 딜레이
        StartCoroutine(DelayedSceneLoad());
    }

    public void OnBackClicked()
    {
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        // 소리 끝나기 전에 씬이 바뀌지 않도록 딜레이
        StartCoroutine(DelayedSceneLoad2());
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
            PlayerData.instance.StartKeepAlive();   // Keep-alive 시작 (서버와 연결 유지)

            // Photon CustomProperties에 닉네임 저장
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["nickname"] = PlayerData.instance.nickname;
            // 수정
            Photon.Pun.PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            debugText.text = "로그인 성공!";
            SceneManager.LoadScene("SelectCharacter");
        }
        else
        {
            string errorMsg = req.downloadHandler?.text ?? "오류 발생";
            string parsedMessage;

            try
            {
                var error = JsonUtility.FromJson<ErrorResponse>(errorMsg);
                parsedMessage = error.message;
            }
            catch
            {
                parsedMessage = "서버 오류";
            }

            debugText.text = "로그인 실패: " + parsedMessage;
        }
    }

    IEnumerator DelayedSceneLoad()
    {
        yield return new WaitForSeconds(0.3f); // 사운드 재생 시간에 따라 조정
        SceneManager.LoadScene("SignUpScene");
    }

    IEnumerator DelayedSceneLoad2()
    {
        yield return new WaitForSeconds(0.3f); // 사운드 재생 시간에 따라 조정
        SceneManager.LoadScene("LoadingScene");
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
    public class ErrorResponse
    {
        public string message;
    }
}