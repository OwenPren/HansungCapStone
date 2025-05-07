using Fusion;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("SO References (Injected by Spawner)")]
    public PlayerDataSO runtimeData;                 // 이 플레이어 전용 SO

    /* ---------- 네트워크 동기화 필드 ---------- */
    [Networked, OnChangedRender(nameof(OnCashChanged))] 
    public int Cash     { get; private set; }
    [Networked, OnChangedRender(nameof(OnHoldingsChanged)), Capacity(16)]
    public NetworkDictionary<SectorType,int> Holdings => default;

    public void OnCashChanged()
    {
        runtimeData.SetMoney(Cash);
    }

    public void OnHoldingsChanged()
    {
        foreach (var kvp in Holdings)
            runtimeData.SetHolding(kvp.Key, kvp.Value);
    }


    /* ---------- 서버 전용 데이터 조작 API ---------- */
    public void AddCash(int amount)
    {
        if (!HasStateAuthority) return;
        Cash += amount;
        runtimeData.SetMoney(Cash);                  // SO 반영
    }

    public void AddStock(SectorType sector, int amount)
    {
        if (!HasStateAuthority) return;

        int cur = Holdings.TryGet(sector, out var v) ? v : 0;
        Holdings.Set(sector, cur + amount);      
        runtimeData.SetHolding(sector, cur + amount);
    }
    /* ---------- 동기화 → SO 초기 반영 ---------- */
    public override void Spawned()
    {
        // 최초 동기화 시 서버‑측 값 → SO 로깅
        if (Object.HasStateAuthority)
        {
            runtimeData.SetMoney(Cash);
            foreach (var kvp in Holdings)
                runtimeData.SetHolding(kvp.Key, kvp.Value);
        }
    }
}
