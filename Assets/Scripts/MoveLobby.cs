using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveLobby : MonoBehaviour
{
    public void OnLogInButtonClicked()
    {
        SceneManager.LoadScene("LobbyScene");

    }
}
