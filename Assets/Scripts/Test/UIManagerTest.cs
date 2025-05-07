// ----------------------------------------------
// UIManagerTest.cs
// ----------------------------------------------
using Fusion;
using TMPro;
using UnityEngine;

public class UIManagerTest : MonoBehaviour
{
    [Header("SO References")]
    [SerializeField] private PlayerDatabaseSO playerDB;

    [Header("UI References")]
    [SerializeField] private TMP_Text moneyText;
    //[SerializeField] private TMP_Text stockText;   // Technology 주식 예시

    private PlayerDataSO _myData;                  // 로컬 플레이어 SO

    private void Start()
    {
        StartCoroutine(WaitForRunner());
    }

    /// <summary>
    /// Runner 초기화 · LocalPlayer 생성까지 기다렸다가 SO 구독
    /// </summary>
    private System.Collections.IEnumerator WaitForRunner()
    {
        // Fusion Runner가 준비될 때까지 대기
        NetworkRunner runner = null;
        while (runner == null)
        {
            runner = FindObjectOfType<NetworkRunner>();
            yield return null;
        }

        // LocalPlayer 등록까지 대기
        while (runner.LocalPlayer == PlayerRef.None)
            yield return null;

        // SO 찾기 & 구독
        _myData = playerDB.Find(runner.LocalPlayer);
        if (_myData == null)
        {
            Debug.LogError("[UIManagerTest] Local PlayerDataSO not found!");
            yield break;
        }

        _myData.OnChanged.AddListener(UpdateUI);
        UpdateUI(_myData);        // 첫 화면 즉시 갱신
    }

    private void OnDestroy()
    {
        if (_myData != null)
            _myData.OnChanged.RemoveListener(UpdateUI);
    }

    /// <summary>
    /// SO 값이 바뀔 때마다 호출되어 HUD & 콘솔 갱신
    /// </summary>
    private void UpdateUI(PlayerDataSO data)
    {
        moneyText.text = $"Money : ₩{data.money:N0}";

        //int tech = data.holdings.TryGetValue(SectorType.Technology, out var v) ? v : 0;
        //stockText.text = $"Tech Stock : {tech}";

        Debug.Log($"[UIManagerTest] Cash={data.money}");
    }
}
