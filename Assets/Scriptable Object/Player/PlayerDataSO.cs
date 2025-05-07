using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Game/Runtime Player Data")]
public class PlayerDataSO : ScriptableObject
{
    [NonSerialized] public Fusion.PlayerRef Owner;              // 네트워크 플레이어
    public int money;                                           // 현금
    public Dictionary<SectorType, int> holdings =               // 보유 주식
        new Dictionary<SectorType, int>();

    /*  --- 변경 알림 이벤트 (UI, 랭킹 시스템 등이 구독) --- */
    [NonSerialized] public UnityEvent<PlayerDataSO> OnChanged = 
        new UnityEvent<PlayerDataSO>();

    /*  --- 서버에서만 호출 --- */
    public void SetMoney(int value)  { money = value;  OnChanged.Invoke(this); }
    public void SetHolding(SectorType sector, int amt)
    {
        holdings[sector] = amt;
        OnChanged.Invoke(this);
    }
}
