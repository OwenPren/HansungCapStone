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
                Debug.LogWarning("ĳ���� ��������Ʈ�� �ҷ����� ���߽��ϴ�. index: " + charIndex);
            }
        }
        else
        {
            Debug.LogWarning("ĳ���� ������ CustomProperties�� �����ϴ�.");
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
