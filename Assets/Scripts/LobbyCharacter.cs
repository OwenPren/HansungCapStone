using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyCharacter : MonoBehaviour
{
    public Image characterImage;
    public Button backButton;
    // Start is called before the first frame update
    void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("character"))
        {
            int charIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["character"];
            string path = "Characters/Character_" + charIndex;
            Sprite characterSprite = Resources.Load<Sprite>(path);

            if (characterSprite != null)
            {
                characterImage.sprite = characterSprite;
            }
            else
            {
                Debug.LogWarning("캐릭터 스프라이트를 불러오지 못했습니다. index: " + charIndex);
            }
        }
        else
        {
            Debug.LogWarning("캐릭터 정보가 CustomProperties에 없습니다.");
        }
    }
    void OnBackClicked()
    {
        SceneManager.LoadScene("SelectCharacter");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
