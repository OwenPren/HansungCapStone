using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject menuPanel;    // �޴� �г��� ����

    // Start is called before the first frame update
    void Start()
    {
        menuPanel.SetActive(false); // ó������ �޴� �г��� ����
    }

    public void ToggleMenu()
    {
        menuPanel.SetActive(!menuPanel.activeSelf);     // ��ư Ŭ�� �� �޴� �г� ǥ��/�����
    }
    public void ExitGame()
    {
        Application.Quit(); // ���� ����
    }
    public void ResetGame()
    {
        SceneManager.LoadScene("LoadingScene");     // ù ��° ������ �̵�
    }

    public void AdjustGameMusic(float volume)
    {
        AudioSource audioSource = FindObjectOfType<AudioSource>();
        if (audioSource != null)
        {
            audioSource.volume = volume; // ���� ���� ����
        }
    }
}
