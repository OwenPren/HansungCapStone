using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : NetworkBehaviour
{
    public static GameUIManager Instance { get; private set; }
    public RoundStartEventSO roundStartEvent;

    private bool isStartGame;

    [Header("GameUI")]
    [SerializeField] private GameObject gameUI;

    [Header("WatingRoomUI")]
    [SerializeField] private GameObject watingRoomUI;
    [SerializeField] private List<Image> playerSlots;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text roomCode;

    [Header("Network Player Management")]
    private List<PlayerRef> _joinOrder = new List<PlayerRef>();

    private void OnEnable()
    {
        roundStartEvent.AddListener(StartGame);
    }

    private void OnDisable()
    {
        roundStartEvent.RemoveListener(StartGame);
    }

    private void Awake()
    {
        // 이미 인스턴스가 있으면 중복 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        HideUI(gameUI);
        ShowUI(watingRoomUI);
        isStartGame = false;
        startButton.interactable = false;
    }

    public override void Spawned()
    {
        // NetworkBehaviour 초기화
        if (Object.HasStateAuthority)
        {
            Debug.Log("[GameUIManager] Network object spawned with authority");
        }
    }

    #region UI Control Methods
    public void ShowUI(GameObject obj)
    {
        obj.SetActive(true);
    }

    public void HideUI(GameObject obj)
    {
        obj.SetActive(false);
    }

    public void ToggleUI(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }

    public void ToggleStartButton()
    {
        startButton.interactable = FindObjectOfType<NetworkRunner>()?.IsServer == true;
    }

    public void StartGame()
    {
        if (isStartGame) return;

        ToggleUI(gameUI);
        ToggleUI(watingRoomUI);

        isStartGame = true;
    }

    public void SetRoomCode(string roomCode)
    {
        this.roomCode.text = roomCode;
    }
    #endregion

    #region Network Player Slot Management
    /// <summary>
    /// 플레이어 슬롯 설정 (로컬에서만 실행)
    /// </summary>
    public void SetPlayerSlots(int slotIndex, Sprite characterSprite)
    {
        if (slotIndex < 0 || slotIndex >= playerSlots.Count) return;

        var img = playerSlots[slotIndex];
        img.sprite = characterSprite;
        img.enabled = true;
        
        Debug.Log($"[GameUIManager] Set player slot {slotIndex} locally");
    }

    /// <summary>
    /// 플레이어 슬롯 클리어 (로컬에서만 실행)
    /// </summary>
    public void ClearPlayerSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= playerSlots.Count) return;

        var img = playerSlots[slotIndex];
        img.sprite = null;
        img.enabled = false;
        
        Debug.Log($"[GameUIManager] Cleared player slot {slotIndex} locally");
    }

    /// <summary>
    /// 플레이어 참가 시 모든 클라이언트에 UI 업데이트 전송
    /// </summary>
    public void OnPlayerJoined(PlayerRef player, int charIndex)
    {
        if (!Object.HasStateAuthority) return;

        if (!_joinOrder.Contains(player))
        {
            _joinOrder.Add(player);
        }

        int slotIndex = _joinOrder.IndexOf(player);
        UpdatePlayerSlotRPC(slotIndex, charIndex);
    }

    /// <summary>
    /// 플레이어 퇴장 시 모든 클라이언트에 UI 업데이트 전송
    /// </summary>
    public void OnPlayerLeft(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        int slotIndex = _joinOrder.IndexOf(player);
        if (slotIndex >= 0)
        {
            _joinOrder.Remove(player);
            ClearPlayerSlotRPC(slotIndex);

            // 남은 플레이어들의 슬롯 재정렬
            SyncAllPlayerSlotsRPC();
        }
    }

    /// <summary>
    /// 모든 클라이언트에게 플레이어 슬롯 UI 업데이트를 전송하는 RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void UpdatePlayerSlotRPC(int slotIndex, int charIndex)
    {
        string path = "Characters/Character_" + charIndex;
        Sprite characterSprite = Resources.Load<Sprite>(path);

        if (characterSprite != null)
        {
            SetPlayerSlots(slotIndex, characterSprite);
            Debug.Log($"[GameUIManager RPC] Player slot {slotIndex} updated with character {charIndex}");
        }
        else
        {
            Debug.LogError($"[GameUIManager RPC] Failed to load character sprite at path: {path}");
        }
    }

    /// <summary>
    /// 모든 클라이언트에게 플레이어 슬롯 제거를 전송하는 RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void ClearPlayerSlotRPC(int slotIndex)
    {
        ClearPlayerSlot(slotIndex);
        Debug.Log($"[GameUIManager RPC] Player slot {slotIndex} cleared");
    }

    /// <summary>
    /// 늦게 접속한 클라이언트를 위해 현재 모든 플레이어의 슬롯 정보를 동기화
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void SyncAllPlayerSlotsRPC()
    {
        if (!Object.HasStateAuthority) return;

        // 모든 슬롯 초기화
        for (int i = 0; i < playerSlots.Count; i++)
        {
            ClearPlayerSlot(i);
        }

        // 현재 접속한 플레이어들의 정보 재설정
        for (int i = 0; i < _joinOrder.Count; i++)
        {
            PlayerRef player = _joinOrder[i];
            
            // 실제 구현에서는 플레이어의 캐릭터 정보를 가져오는 방법이 필요
            // 예시: PlayerManager나 다른 컴포넌트에서 캐릭터 인덱스 조회
            if (TryGetPlayerCharacterIndex(player, out int charIndex))
            {
                UpdatePlayerSlotRPC(i, charIndex);
            }
        }
    }

    /// <summary>
    /// 플레이어의 캐릭터 인덱스를 가져오는 헬퍼 메서드
    /// (실제 구현에 맞게 수정 필요)
    /// </summary>
    private bool TryGetPlayerCharacterIndex(PlayerRef player, out int charIndex)
    {
        charIndex = 0;
        
        // GameManager에서 PlayerManager 찾기
        if (GameManager.Instance != null)
        {
            // GameManager의 playerManagers에서 해당 플레이어 찾기
            // 실제 구현에서는 PlayerManager에 캐릭터 정보를 저장해야 함
            // 임시로 PUN 정보 사용 (실제로는 네트워크 동기화된 데이터 사용 권장)
            if (Photon.Pun.PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("character"))
            {
                charIndex = (int)Photon.Pun.PhotonNetwork.LocalPlayer.CustomProperties["character"];
                return true;
            }
        }
        
        return false;
    }
    #endregion
}