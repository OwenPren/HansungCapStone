using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject menuPanel;    // 메뉴 패널을 참조

    // Start is called before the first frame update
    void Start()
    {
        menuPanel.SetActive(false); // 처음에는 메뉴 패널을 숨김
    }

    public void ToggleMenu()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);     // 버튼 클릭 시 메뉴 패널 표시/숨기기
    }
    public void ExitGame()
    {
        Application.Quit(); // 게임 종료
    }
    public void ResetGame()
    {
        SceneManager.LoadScene("LoadingScene");     // 첫 번째 씬으로 이동
    }

    public void AdjustGameMusic(float volume)
    {
        AudioSource audioSource = FindObjectOfType<AudioSource>();
        if (audioSource != null)
        {
            audioSource.volume = volume; // 음악 볼륨 조절
        }
    }
}
