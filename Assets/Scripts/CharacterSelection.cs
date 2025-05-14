using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelection : MonoBehaviour
{
    public TMP_Text debugText;  // 아이디 닉네임 캐릭터 디버깅 출력용
    public Image characterDisplay;
    //public Sprite[] characterSprites; // 캐릭터 이미지 배열
    public int maxCharacters = 4;
    private int currentIndex = 0;
    public Button backButton;

    void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        UpdateCharacterDisplay();
        // 디버깅용 출력
        debugText.text = $"[디버그]\n아이디: {PlayerData.instance.userID}\n닉네임: {PlayerData.instance.nickname}\n캐릭터: {PlayerData.instance.selectedCharacterIndex}";
    }

    public void NextCharacter()
    {
        currentIndex = (currentIndex + 1) % maxCharacters;
        UpdateCharacterDisplay();
    }

    public void PreviousCharacter()
    {
        currentIndex = (currentIndex - 1 + maxCharacters) % maxCharacters;
        UpdateCharacterDisplay();
    }

    private void UpdateCharacterDisplay()
    {
        string path = $"Characters/Character_{currentIndex}";
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab != null)
        {
            SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                characterDisplay.sprite = sr.sprite;
            }
            else
            {
                Debug.LogWarning("SpriteRenderer not found in prefab.");
            }
        }
        else
        {
            Debug.LogWarning($"Character prefab not found at path: {path}");
        }
        // 캐릭터 선택 변경 시 디버깅 텍스트 갱신
        debugText.text = $"아이디: {PlayerData.instance.userID}\n닉네임: {PlayerData.instance.nickname}\n현재 캐릭터: {currentIndex}";
    }

    public void OnDone()
    {
        // 선택한 캐릭터 정보 저장
        PlayerData.instance.selectedCharacterIndex = currentIndex;
        // 캐릭터 정보도 Photon CustomProperties에 저장
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["character"] = currentIndex;
        Photon.Pun.PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        // 다음 씬 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public void OnBackClicked()
    {
        SceneManager.LoadScene("LogInScene");
    }
}