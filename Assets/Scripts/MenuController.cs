using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject menuPanel;    // 메뉴 패널 객체
    private string serverBaseUrl = "http://43.203.206.157:3000";    // 서버 주소

    void Start()
    {
        menuPanel.SetActive(false); // 처음에는 메뉴 패널 비활성화
    }

    public void ToggleMenu()
    {
        menuPanel.SetActive(!menuPanel.activeSelf); // 버튼 클릭 시 메뉴 패널 토글
    }

    public void ExitGame()
    {
        StartCoroutine(Logout());
        //Application.Quit(); // 게임 종료
    }

    public void ResetGame()
    {
        SceneManager.LoadScene("LoadingScene"); // 첫 번째 씬으로 이동
    }

    public void AdjustGameMusic(float volume)
    {
        AudioSource audioSource = FindObjectOfType<AudioSource>();
        if (audioSource != null)
        {
            audioSource.volume = volume; // 음악 볼륨 조정
        }
    }
    IEnumerator Logout()
    {
        string id = PlayerData.instance.userID;
        string json = $"{{\"username\":\"{id}\"}}";

        UnityWebRequest req = new UnityWebRequest($"{serverBaseUrl}/logout", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("로그아웃 실패: " + req.error);
        }
        else
        {
            Debug.Log("로그아웃 완료");
        }

        Application.Quit();
    }
}