using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class MoveGame : MonoBehaviour
{
    public void OnSelectButtonClicked()
    {
        SceneManager.LoadScene("GameScene");
    }
}
