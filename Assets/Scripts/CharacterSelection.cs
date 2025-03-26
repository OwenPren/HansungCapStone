using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    public Button leftButton;
    public Button rightButton;
    public Image[] characterImages;
    public string[] characterNames;    // 캐릭터 이름을 저장할 배열 추가
    private int selectedIndex = 0;

    private void Start()
    {
        UpdateCharacterSelection();
        leftButton.onClick.AddListener(SelectPreviousCharacter);
        rightButton.onClick.AddListener(SelectNextCharacter);
    }

    private void SelectPreviousCharacter()
    {
        selectedIndex = (selectedIndex - 1 + characterImages.Length) % characterImages.Length;
        UpdateCharacterSelection();
    }

    private void SelectNextCharacter()
    {
        selectedIndex = (selectedIndex + 1) % characterImages.Length;
        UpdateCharacterSelection();
    }

    private void UpdateCharacterSelection()
    {
        for (int i = 0; i < characterImages.Length; i++)
        {
            characterImages[i].color = (i == selectedIndex) ? Color.yellow : Color.white;
        }
    }

        public void ConfirmSelectionAndLoadNextScene()
    {
        // PlayerPrefs를 사용하여 캐릭터 정보 저장
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedIndex);
        PlayerPrefs.SetString("SelectedCharacterName", characterNames[selectedIndex]);
        //SceneManager.LoadScene("GameScene");
    }
}
