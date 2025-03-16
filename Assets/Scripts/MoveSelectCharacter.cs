using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveSelectCharacter : MonoBehaviour
{
    public void OnLogInButtonClicked()
    {
        SceneManager.LoadScene("SelectCharacter");

    }
}
