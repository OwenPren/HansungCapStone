// CashAutoTicker.cs
using Fusion;
using UnityEngine;

/// <summary>
/// 서버가 3초마다 모든 플레이어의 Cash 를 증가시켜
/// 네트워크 동기화 & UI 미러링이 잘 되는지 확인하는 디버그용 컴포넌트.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class CashAutoTicker : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int deltaCash   = 100;   // 증가량
    [SerializeField] private float interval = 3f;     // 초 단위

    private TickTimer _timer;

    /* ---------------- 초기화 ---------------- */
    public override void Spawned()
    {
        if (Object.HasStateAuthority)                // Host 전용
            _timer = TickTimer.CreateFromSeconds(Runner, interval);
    }

    /* ---------------- 매 틱 실행 ---------------- */
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;       // 서버만 작동

        if (_timer.Expired(Runner))
        {
            IncreaseAllPlayersCash();
            _timer = TickTimer.CreateFromSeconds(Runner, interval); // 타이머 리셋
        }
    }

    /* ---------------- 핵심 로직 ---------------- */
    private void IncreaseAllPlayersCash()
    {
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out var obj))
            {
                var pn = obj.GetComponent<PlayerNetwork>();
                pn.AddCash(deltaCash);               // PlayerNetwork API 호출
            }
        }
        Debug.Log($"[CashAutoTicker] +{deltaCash} to every player at {Time.time:F1}s");
    }
}
