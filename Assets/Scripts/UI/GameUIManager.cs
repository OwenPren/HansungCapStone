using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    private static GameUIManager _instance;
    public static GameUIManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameUIManager>();
                if (_instance == null)
                {
                    Debug.LogError("[GameUIManager] No GameUIManager found in scene!");
                }
                else
                {
                    Debug.Log("[GameUIManager] Instance found via FindObjectOfType");
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }
    
    public RoundStartEventSO roundStartEvent;
    private bool isStartGame;

    [Header("GameUI")]
    [SerializeField] private GameObject gameUI;

    [Header("WatingRoomUI")]  
    [SerializeField] private GameObject watingRoomUI;
    [SerializeField] private List<Image> playerSlots;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text roomCode;

    [Header("Player Management")]
    private Dictionary<PlayerRef, int> playerSlotMapping;

    private void Awake()
    {
        Debug.Log("[GameUIManager] Awake() called");
        
        // 싱글톤 패턴 구현
        if (_instance != null && _instance != this)
        {
            Debug.Log($"[GameUIManager] Destroying duplicate instance. Existing: {_instance.name}, This: {this.name}");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        
        // playerSlotMapping 초기화
        playerSlotMapping = new Dictionary<PlayerRef, int>();
        
        // DontDestroyOnLoad는 필요한 경우에만 사용
        // 현재 씬에만 있어야 할 UI라면 주석 처리
        // DontDestroyOnLoad(gameObject);
        
        Debug.Log("[GameUIManager] Instance set successfully");
    }

    private void OnEnable()
    {
        if (roundStartEvent != null)
        {
            roundStartEvent.AddListener(StartGame);
        }
    }

    private void OnDisable()
    {
        if (roundStartEvent != null)
        {
            roundStartEvent.RemoveListener(StartGame);
        }
    }

    void Start()
    {
        Debug.Log("[GameUIManager] Start() called");
        
        // UI 컴포넌트들 null 체크
        if (gameUI == null) Debug.LogError("[GameUIManager] gameUI is not assigned!");
        if (watingRoomUI == null) Debug.LogError("[GameUIManager] watingRoomUI is not assigned!");
        if (playerSlots == null || playerSlots.Count == 0) Debug.LogError("[GameUIManager] playerSlots is not properly assigned!");
        if (startButton == null) Debug.LogError("[GameUIManager] startButton is not assigned!");
        if (roomCode == null) Debug.LogError("[GameUIManager] roomCode is not assigned!");
        
        // 초기 UI 설정
        if (gameUI != null) HideUI(gameUI);
        if (watingRoomUI != null) ShowUI(watingRoomUI);
        
        isStartGame = false;
        
        if (startButton != null)
        {
            startButton.interactable = false;
        }
        
        // PlayerInfoManager가 준비되면 동기화 시작
        StartCoroutine(WaitForInitialSync());
        
        Debug.Log("[GameUIManager] Start() completed");
    }
    
    private IEnumerator WaitForInitialSync()
    {
        // PlayerInfoManager와 네트워크 준비까지 대기
        yield return new WaitUntil(() => PlayerInfoManager.Instance != null);
        yield return new WaitForSeconds(2.0f); // 충분한 지연으로 모든 플레이어 정보가 준비될 때까지 대기
        
        Debug.Log("[GameUIManager] Performing initial sync");
        SyncAllPlayerSlots();
        
        // 시작 버튼 설정
        ToggleStartButton();
    }

    #region UI Control Methods
    public void ShowUI(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(true);
        }
    }

    public void HideUI(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    public void ToggleUI(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }

    public void ToggleStartButton()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (startButton != null && runner != null)
        {
            startButton.interactable = runner.IsServer;
            
            // 버튼 클릭 이벤트 연결 (서버에서만)
            if (runner.IsServer)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartButtonClicked);
                Debug.Log("[GameUIManager] Start button click listener added for server");
            }
        }
    }

    // 게임 시작 버튼 클릭 시 호출 (서버에서만)
    public void OnStartButtonClicked()
    {
        Debug.Log("[GameUIManager] Start button clicked on server");
        
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && runner.IsServer)
        {
            // 모든 클라이언트에 게임 시작 알림
            RequestGameStart();
        }
    }

    // 게임 시작 요청 (서버에서 모든 클라이언트로)
    public void RequestGameStart()
    {
        // PlayerInfoManager를 통해 RPC 전송
        if (PlayerInfoManager.Instance != null && PlayerInfoManager.Instance.Object.HasStateAuthority)
        {
            Debug.Log("[GameUIManager] Requesting game start via PlayerInfoManager");
            PlayerInfoManager.Instance.RpcStartGame();
        }
        else
        {
            Debug.LogError("[GameUIManager] Cannot request game start - PlayerInfoManager not available or no authority");
        }
    }

    // 실제 게임 시작 처리 (모든 클라이언트에서 실행)
    public void StartGame()
    {
        if (isStartGame) 
        {
            Debug.Log("[GameUIManager] Game already started, ignoring");
            return;
        }

        Debug.Log("[GameUIManager] Starting game - switching UI");

        // UI 전환
        HideUI(watingRoomUI);
        ShowUI(gameUI);

        isStartGame = true;
        
        // 이벤트 발생 (다른 시스템들에게 알림)
        // if (roundStartEvent != null)
        // {
        //     roundStartEvent.Invoke();
        // }

        Debug.Log("[GameUIManager] Game started successfully");
    }

    public void SetRoomCode(string roomCodeText)
    {
        if (roomCode != null)
        {
            this.roomCode.text = roomCodeText;
        }
    }
    #endregion

    #region Player Slot Management
    public void SetPlayerSlots(int slotIndex, Sprite characterSprite)
    {
        Debug.Log($"[GameUIManager] SetPlayerSlots called - slotIndex: {slotIndex}, sprite: {characterSprite?.name}");
        
        if (playerSlots == null)
        {
            Debug.LogError("[GameUIManager] playerSlots list is null!");
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= playerSlots.Count)
        {
            Debug.LogError($"[GameUIManager] Invalid slot index: {slotIndex}, playerSlots count: {playerSlots.Count}");
            return;
        }

        var img = playerSlots[slotIndex];
        if (img != null)
        {
            img.sprite = characterSprite;
            img.enabled = true;
            Debug.Log($"[GameUIManager] Successfully set player slot {slotIndex} with sprite: {characterSprite?.name}");
        }
        else
        {
            Debug.LogError($"[GameUIManager] Image component at slot {slotIndex} is null!");
        }
    }

    public void ClearPlayerSlot(int slotIndex)
    {
        if (playerSlots == null || slotIndex < 0 || slotIndex >= playerSlots.Count) return;

        var img = playerSlots[slotIndex];
        if (img != null)
        {
            img.sprite = null;
            img.enabled = false;
            Debug.Log($"[GameUIManager] Cleared player slot {slotIndex}");
        }
    }

    public void OnPlayerInfoUpdated(PlayerRef player, int characterIndex)
    {
        Debug.Log($"[GameUIManager] OnPlayerInfoUpdated - Player: {player}, CharIndex: {characterIndex}");
        
        if (playerSlotMapping == null)
        {
            Debug.LogError("[GameUIManager] playerSlotMapping is null! Reinitializing...");
            playerSlotMapping = new Dictionary<PlayerRef, int>();
        }
        
        int slotIndex = GetOrAssignPlayerSlot(player);
        Debug.Log($"[GameUIManager] Assigned slot {slotIndex} to player {player}");
        
        string path = "Characters/Character_" + characterIndex;
        Sprite characterSprite = Resources.Load<Sprite>(path);
        
        if (characterSprite != null)
        {
            SetPlayerSlots(slotIndex, characterSprite);
            Debug.Log($"[GameUIManager] Successfully updated slot {slotIndex} with character {characterIndex}");
        }
        else
        {
            Debug.LogError($"[GameUIManager] Failed to load character sprite at path: {path}");
        }
    }

    private int GetOrAssignPlayerSlot(PlayerRef player)
    {
        if (playerSlotMapping == null)
        {
            playerSlotMapping = new Dictionary<PlayerRef, int>();
        }
        
        if (playerSlotMapping.TryGetValue(player, out int existingSlot))
        {
            return existingSlot;
        }

        int newSlot = playerSlotMapping.Count;
        playerSlotMapping[player] = newSlot;
        
        Debug.Log($"[GameUIManager] New slot {newSlot} assigned to player {player}");
        return newSlot;
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (playerSlotMapping != null && playerSlotMapping.TryGetValue(player, out int slotIndex))
        {
            playerSlotMapping.Remove(player);
            ClearPlayerSlot(slotIndex);
            Debug.Log($"[GameUIManager] Player {player} left, cleared slot {slotIndex}");
        }
    }

    public void SyncAllPlayerSlots()
    {
        Debug.Log("[GameUIManager] SyncAllPlayerSlots called");

        if (playerSlotMapping == null)
        {
            playerSlotMapping = new Dictionary<PlayerRef, int>();
        }

        // 모든 슬롯 초기화
        ClearAllSlots();
        playerSlotMapping.Clear();

        // PlayerInfoManager 확인
        if (PlayerInfoManager.Instance == null)
        {
            Debug.LogWarning("[GameUIManager] PlayerInfoManager.Instance is null during sync");
            return;
        }

        Debug.Log($"[GameUIManager] Found {PlayerInfoManager.Instance.PlayerInfos.Count} players to sync");
        
        foreach (var kvp in PlayerInfoManager.Instance.PlayerInfos)
        {
            PlayerRef player = kvp.Key;
            NetworkPlayerInfo playerInfo = kvp.Value;
            
            int slotIndex = GetOrAssignPlayerSlot(player);
            
            string path = "Characters/Character_" + playerInfo.selectedCharacterIndex;
            Sprite characterSprite = Resources.Load<Sprite>(path);
            
            if (characterSprite != null)
            {
                SetPlayerSlots(slotIndex, characterSprite);
                Debug.Log($"[GameUIManager] Synced player {player} ('{playerInfo.nickname.ToString()}') to slot {slotIndex} with character {playerInfo.selectedCharacterIndex}");
            }
            else
            {
                Debug.LogError($"[GameUIManager] Failed to load character sprite: {path}");
            }
        }
    }

    public void ClearAllSlots()
    {
        if (playerSlots == null) return;
        
        Debug.Log("[GameUIManager] Clearing all player slots");
        for (int i = 0; i < playerSlots.Count; i++)
        {
            ClearPlayerSlot(i);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log($"[GameUIManager] === UI DEBUG INFO ===");
            Debug.Log($"[GameUIManager] Instance: {(_instance != null ? "EXISTS" : "NULL")}");
            Debug.Log($"[GameUIManager] playerSlotMapping: {(playerSlotMapping != null ? $"EXISTS (Count: {playerSlotMapping.Count})" : "NULL")}");
            Debug.Log($"[GameUIManager] playerSlots: {(playerSlots != null ? $"EXISTS (Count: {playerSlots.Count})" : "NULL")}");
            
            if (playerSlotMapping != null)
            {
                foreach (var kvp in playerSlotMapping)
                {
                    Debug.Log($"[GameUIManager] Player {kvp.Key} -> Slot {kvp.Value}");
                }
            }
            
            if (playerSlots != null)
            {
                for (int i = 0; i < playerSlots.Count; i++)
                {
                    var slot = playerSlots[i];
                    if (slot != null)
                    {
                        Debug.Log($"[GameUIManager] Slot {i}: enabled={slot.enabled}, sprite={slot.sprite?.name ?? "null"}");
                    }
                    else
                    {
                        Debug.Log($"[GameUIManager] Slot {i}: NULL IMAGE COMPONENT");
                    }
                }
            }
            
            Debug.Log("[GameUIManager] Triggering manual sync...");
            SyncAllPlayerSlots();
        }
        
        // 게임 시작 테스트 키 (G키 - 서버에서만)
        if (Input.GetKeyDown(KeyCode.G))
        {
            var runner = FindObjectOfType<NetworkRunner>();
            if (runner != null && runner.IsServer)
            {
                Debug.Log("[GameUIManager] Manual game start triggered (G key)");
                RequestGameStart();
            }
            else
            {
                Debug.Log("[GameUIManager] G key pressed but not server or no runner");
            }
        }
    }
    #endregion
}