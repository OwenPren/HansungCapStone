using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseButton : MonoBehaviour
{
    public void OnCloseButtonClick()
    {
        //UIManager uiManager = UIManager.Instance; // UIManager.Instance는 GameManager처럼 싱글톤으로 가정합니다.
        //if (uiManager != null)
        //{
        //    gameObject.SetActive(false); // 일단 이 패널만 비활성화
        //    uiManager.ShowMarketPanel(); // UIManager에 MarketPanel (목록)을 보여주는 메서드가 있다고 가정
        //}
        //else
        //{
        //Debug.LogError("UIManager instance not found!");
        gameObject.SetActive(false); // UIManager 없으면 일단 패널 닫기
        //}
    }
}
