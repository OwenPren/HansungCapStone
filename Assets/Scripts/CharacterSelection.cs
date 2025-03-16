using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    public Image characterDisplay;
    public Sprite[] characterSprites; // 캐릭터 이미지 배열
    private int currentIndex = 0;

    void Start()
    {
        UpdateCharacterDisplay();
    }

    public void NextCharacter()
    {
        currentIndex = (currentIndex + 1) % characterSprites.Length;
        UpdateCharacterDisplay();
    }

    public void PreviousCharacter()
    {
        currentIndex = (currentIndex - 1 + characterSprites.Length) % characterSprites.Length;
        UpdateCharacterDisplay();
    }

    private void UpdateCharacterDisplay()
    {
        if (characterSprites.Length > 0)
        {
            characterDisplay.sprite = characterSprites[currentIndex];
        }
    }

    public void OnDone()
    {
        // 선택한 캐릭터 정보 저장
        PlayerPrefs.SetInt("SelectedCharacterIndex", currentIndex);
        // 다음 씬 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }
}