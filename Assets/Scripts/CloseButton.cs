using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseButton : MonoBehaviour
{
    public void OnCloseButtonClick()
    {
        //UIManager uiManager = UIManager.Instance; // UIManager.Instance�� GameManageró�� �̱������� �����մϴ�.
        //if (uiManager != null)
        //{
        //    gameObject.SetActive(false); // �ϴ� �� �гθ� ��Ȱ��ȭ
        //    uiManager.ShowMarketPanel(); // UIManager�� MarketPanel (���)�� �����ִ� �޼��尡 �ִٰ� ����
        //}
        //else
        //{
        //Debug.LogError("UIManager instance not found!");
        gameObject.SetActive(false); // UIManager ������ �ϴ� �г� �ݱ�
        //}
    }
}
