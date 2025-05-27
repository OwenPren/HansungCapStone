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

    public AudioClip clickSound;
    private AudioSource audioSource;

    [Header("GameUI")]
    [SerializeField] private GameObject gameUI;

    [Header("WatingRoomUI")]  
    [SerializeField] private GameObject watingRoomUI;
    [SerializeField] private List<Image> playerSlots;
    [SerializeField] private List<TMP_Text> playerNames;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text roomCode;

    [Header("Assistant Status")]
    [SerializeField] private GameObject assistantStatusPanel;
    [SerializeField] private TMP_Text assistantStatusText;
    [SerializeField] private Image assistantStatusIcon;
    [SerializeField] private Slider assistantProgressBar;

    [Header("Player Management")]
    private Dictionary<PlayerRef, int> playerSlotMapping;

    // Assistant 상태 관리
    private bool isAssistantReady = false;
    private bool isCheckingAssistantStatus = false;

    private void Awake()
    {
        Debug.Log("[GameUIManager] Awake() called");
        
        if (_instance != null && _instance != this)
        {
            Debug.Log($"[GameUIManager] Destroying duplicate instance. Existing: {_instance.name}, This: {this.name}");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        playerSlotMapping = new Dictionary<PlayerRef, int>();
        
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
        audioSource = GetComponent<AudioSource>();
        Debug.Log("[GameUIManager] Start() called");
        
        // UI 컴포넌트들 null 체크
        if (gameUI == null) Debug.LogError("[GameUIManager] gameUI is not assigned!");
        if (watingRoomUI == null) Debug.LogError("[GameUIManager] watingRoomUI is not assigned!");
        if (playerSlots == null || playerSlots.Count == 0) Debug.LogError("[GameUIManager] playerSlots is not properly assigned!");
        if (playerNames == null || playerNames.Count == 0) Debug.LogError("[GameUIManager] playerNames is not properly assigned!");
        if (startButton == null) Debug.LogError("[GameUIManager] startButton is not assigned!");
        if (roomCode == null) Debug.LogError("[GameUIManager] roomCode is not assigned!");
        if (assistantStatusPanel == null) Debug.LogError("[GameUIManager] assistantStatusPanel is not assigned!");
        if (assistantStatusText == null) Debug.LogError("[GameUIManager] assistantStatusText is not assigned!");
        
        // 초기 UI 설정
        if (gameUI != null) HideUI(gameUI);
        if (watingRoomUI != null) ShowUI(watingRoomUI);
        
        isStartGame = false;
        isAssistantReady = false;
        
        // Assistant 상태 패널 초기 설정
        UpdateAssistantStatus("Assistant 초기화 중...", false);
        
        // 시작 버튼 초기 비활성화
        if (startButton != null)
        {
            startButton.interactable = false;
        }
        
        // 모든 플레이어 슬롯과 이름 초기화
        ClearAllSlots();
        
        // PlayerInfoManager가 준비되면 동기화 시작
        StartCoroutine(WaitForInitialSync());
        
        Debug.Log("[GameUIManager] Start() completed");
    }
    
    private IEnumerator WaitForInitialSync()
    {
        // PlayerInfoManager와 네트워크 준비까지 대기
        yield return new WaitUntil(() => PlayerInfoManager.Instance != null);
        yield return new WaitForSeconds(2.0f);
        
        Debug.Log("[GameUIManager] Performing initial sync");
        SyncAllPlayerSlots();
        
        // Assistant 상태 체크 시작
        StartCoroutine(CheckAssistantStatus());
        
        // 시작 버튼 설정 (Assistant 준비 상태에 따라)
        ToggleStartButton();
    }

    // Assistant 상태 체크 코루틴 (클라이언트/서버 모두에서 실행)
    private IEnumerator CheckAssistantStatus()
    {
        if (isCheckingAssistantStatus) yield break;
        
        isCheckingAssistantStatus = true;
        Debug.Log("[GameUIManager] Starting Assistant status check");
        
        UpdateAssistantStatus("스레드 생성 중...", false);
        
        // AssistantManager가 준비될 때까지 대기
        yield return new WaitUntil(() => AssistantManager.Instance != null);
        
        if (AssistantManager.Instance == null)
        {
            Debug.LogError("[GameUIManager] AssistantManager.Instance not found!");
            UpdateAssistantStatus("Assistant 오류", false);
            yield break;
        }
        
        // 네트워크 러너 확인
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[GameUIManager] NetworkRunner not found!");
            UpdateAssistantStatus("네트워크 오류", false);
            yield break;
        }
        
        Debug.Log($"[GameUIManager] Starting status monitoring on {(runner.IsServer ? "SERVER" : "CLIENT")}");
        
        // Assistant 상태를 주기적으로 체크
        float timeout = 60f; // 60초 타임아웃
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // 서버와 클라이언트 모두에서 동일한 방식으로 상태 확인
            AssistantStatus status = AssistantManager.Instance.GetCurrentStatus();
            string message = AssistantManager.Instance.GetStatusMessage();
            float progress = AssistantManager.Instance.GetProgress();
            bool isRound1Ready = AssistantManager.Instance.IsRoundEventsReady(1);
            
            Debug.Log($"[GameUIManager] Status check - Status: {status}, Message: '{message}', Round1Ready: {isRound1Ready}, Progress: {progress:F2}");
            
            // UI 업데이트
            UpdateAssistantStatus(message, status == AssistantStatus.Ready && isRound1Ready);
            UpdateAssistantProgress(progress);
            
            // 준비 완료 체크
            if (status == AssistantStatus.Ready && isRound1Ready)
            {
                Debug.Log("[GameUIManager] Assistant is ready!");
                isAssistantReady = true;
                ToggleStartButton(); // 버튼 상태 업데이트
                break;
            }
            
            yield return new WaitForSeconds(1f); // 1초마다 체크
            elapsed += 1f;
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogError("[GameUIManager] Assistant status check timeout!");
            UpdateAssistantStatus("시간 초과 (계속 진행 가능)", false);
            isAssistantReady = true; // 타임아웃 시에도 게임 진행 허용
            ToggleStartButton();
        }
        
        isCheckingAssistantStatus = false;
        
        // 5초 후에 Assistant 상태 패널 숨기기
        if (isAssistantReady)
        {
            yield return new WaitForSeconds(5f);
            if (assistantStatusPanel != null)
            {
                assistantStatusPanel.SetActive(false);
            }
        }
    }

    // AssistantManager RPC에서 호출되는 메서드
    public void OnAssistantReady()
    {
        Debug.Log("[GameUIManager] OnAssistantReady called");
        isAssistantReady = true;
        ToggleStartButton();
    }

    // Assistant 상태 업데이트
    public void UpdateAssistantStatus(string statusText, bool isReady)
    {
        Debug.Log($"[GameUIManager] UpdateAssistantStatus: {statusText}, Ready: {isReady}");
        
        if (assistantStatusText != null)
        {
            assistantStatusText.text = statusText;
        }
        
        if (assistantStatusIcon != null)
        {
            // 준비 상태에 따라 아이콘 색상 변경
            assistantStatusIcon.color = isReady ? Color.green : Color.yellow;
        }
        
        if (assistantStatusPanel != null)
        {
            assistantStatusPanel.SetActive(true);
        }

        // 준비 완료 시 isAssistantReady 상태 업데이트
        if (isReady && !isAssistantReady)
        {
            isAssistantReady = true;
            ToggleStartButton();
        }
    }

    // Assistant 진행률 업데이트
    public void UpdateAssistantProgress(float progress)
    {
        if (assistantProgressBar != null)
        {
            assistantProgressBar.value = progress;
        }
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
            // 서버이면서 Assistant가 준비된 경우에만 버튼 활성화
            bool shouldEnable = runner.IsServer && isAssistantReady;
            startButton.interactable = shouldEnable;
            
            Debug.Log($"[GameUIManager] Start button state - IsServer: {runner.IsServer}, AssistantReady: {isAssistantReady}, ButtonEnabled: {shouldEnable}");
            
            // 버튼 클릭 이벤트 연결 (서버에서만)
            if (runner.IsServer && isAssistantReady)
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
        // 클릭 사운드 재생
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        Debug.Log("[GameUIManager] Start button clicked on server");
        
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && runner.IsServer && isAssistantReady)
        {
            // 모든 클라이언트에 게임 시작 알림
            RequestGameStart();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] Cannot start game - conditions not met");
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
    // 수정된 메서드: 캐릭터 스프라이트와 이름을 함께 설정
    public void SetPlayerSlots(int slotIndex, Sprite characterSprite, string playerName = "")
    {
        Debug.Log($"[GameUIManager] SetPlayerSlots called - slotIndex: {slotIndex}, sprite: {characterSprite?.name}, name: {playerName}");
        
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

        // 캐릭터 이미지 설정
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

        // 플레이어 이름 설정
        if (playerNames != null && slotIndex < playerNames.Count)
        {
            var nameText = playerNames[slotIndex];
            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(playerName) ? $"name{slotIndex + 1}" : playerName;
                nameText.gameObject.SetActive(true);
                Debug.Log($"[GameUIManager] Successfully set player name {slotIndex} to: {nameText.text}");
            }
            else
            {
                Debug.LogError($"[GameUIManager] Text component at slot {slotIndex} is null!");
            }
        }
    }

    public void ClearPlayerSlot(int slotIndex)
    {
        if (playerSlots == null || slotIndex < 0 || slotIndex >= playerSlots.Count) return;

        // 캐릭터 이미지 초기화
        var img = playerSlots[slotIndex];
        if (img != null)
        {
            img.sprite = null;
            img.enabled = false;
            Debug.Log($"[GameUIManager] Cleared player slot {slotIndex}");
        }

        // 플레이어 이름 초기화
        if (playerNames != null && slotIndex < playerNames.Count)
        {
            var nameText = playerNames[slotIndex];
            if (nameText != null)
            {
                nameText.text = $"name{slotIndex + 1}";
                nameText.gameObject.SetActive(false);
                Debug.Log($"[GameUIManager] Cleared player name {slotIndex}");
            }
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
        
        // 캐릭터 스프라이트 로드
        string path = "Characters/Character_" + characterIndex;
        Sprite characterSprite = Resources.Load<Sprite>(path);
        
        // 플레이어 이름 가져오기
        string playerName = GetPlayerName(player);
        
        if (characterSprite != null)
        {
            SetPlayerSlots(slotIndex, characterSprite, playerName);
            Debug.Log($"[GameUIManager] Successfully updated slot {slotIndex} with character {characterIndex} and name {playerName}");
        }
        else
        {
            Debug.LogError($"[GameUIManager] Failed to load character sprite at path: {path}");
        }
    }

    // 플레이어 이름을 가져오는 헬퍼 메서드
    private string GetPlayerName(PlayerRef player)
    {
        string playerName = "";
        
        // PlayerInfoManager에서 플레이어 정보 가져오기
        if (PlayerInfoManager.Instance != null)
        {
            var playerInfo = PlayerInfoManager.Instance.GetPlayerInfo(player);
            if (playerInfo.HasValue)
            {
                playerName = playerInfo.Value.nickname.ToString();
                Debug.Log($"[GameUIManager] Found player name from PlayerInfoManager: {playerName}");
            }
        }
        
        // PlayerInfoManager에서 못 찾았으면 GameManager를 통해 PlayerManager에서 찾기
        if (string.IsNullOrEmpty(playerName) && GameManager.Instance != null)
        {
            var playerManager = GameManager.Instance.GetPlayerManager(player);
            if (playerManager != null)
            {
                playerName = playerManager.NameField;
                Debug.Log($"[GameUIManager] Found player name from PlayerManager: {playerName}");
            }
        }
        
        // 여전히 없으면 기본값 사용
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = $"Player{player.RawEncoded}";
            Debug.Log($"[GameUIManager] Using default player name: {playerName}");
        }
        
        return playerName;
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
        Debug.Log($"[GameUIManager] OnPlayerLeft called for player {player}");
        
        if (playerSlotMapping != null && playerSlotMapping.TryGetValue(player, out int slotIndex))
        {
            playerSlotMapping.Remove(player);
            ClearPlayerSlot(slotIndex);
            Debug.Log($"[GameUIManager] Player {player} left, cleared slot {slotIndex}");
        }
        else
        {
            Debug.LogWarning($"[GameUIManager] Player {player} not found in slot mapping");
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
        
        // PlayerInfos의 복사본을 만들어 안전하게 순회
        var playerInfosCopy = new List<(PlayerRef, NetworkPlayerInfo)>();
        
        try
        {
            foreach (var kvp in PlayerInfoManager.Instance.PlayerInfos)
            {
                playerInfosCopy.Add((kvp.Key, kvp.Value));
            }
        }
        catch (System.InvalidOperationException)
        {
            Debug.LogWarning("[GameUIManager] PlayerInfos dictionary was modified during enumeration, retrying...");
            // 짧은 지연 후 재시도
            Invoke(nameof(SyncAllPlayerSlots), 0.1f);
            return;
        }
        
        // 복사본을 사용하여 UI 업데이트
        foreach (var (player, playerInfo) in playerInfosCopy)
        {
            int slotIndex = GetOrAssignPlayerSlot(player);
            
            string path = "Characters/Character_" + playerInfo.selectedCharacterIndex;
            Sprite characterSprite = Resources.Load<Sprite>(path);
            string playerName = playerInfo.nickname.ToString();
            
            if (characterSprite != null)
            {
                SetPlayerSlots(slotIndex, characterSprite, playerName);
                Debug.Log($"[GameUIManager] Synced player {player} ('{playerName}') to slot {slotIndex} with character {playerInfo.selectedCharacterIndex}");
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
            Debug.Log($"[GameUIManager] isAssistantReady: {isAssistantReady}");
            Debug.Log($"[GameUIManager] playerSlotMapping: {(playerSlotMapping != null ? $"EXISTS (Count: {playerSlotMapping.Count})" : "NULL")}");
            Debug.Log($"[GameUIManager] playerSlots: {(playerSlots != null ? $"EXISTS (Count: {playerSlots.Count})" : "NULL")}");
            Debug.Log($"[GameUIManager] playerNames: {(playerNames != null ? $"EXISTS (Count: {playerNames.Count})" : "NULL")}");
            
            // Assistant 상태 체크
            if (AssistantManager.Instance != null)
            {
                Debug.Log($"[GameUIManager] Assistant Status: {AssistantManager.Instance.GetCurrentStatus()}");
                Debug.Log($"[GameUIManager] Assistant Message: '{AssistantManager.Instance.GetStatusMessage()}'");
                Debug.Log($"[GameUIManager] Round 1 Events Ready: {AssistantManager.Instance.IsRoundEventsReady(1)}");
                AssistantManager.Instance.LogRoundEventsStatus();
            }
            else
            {
                Debug.LogWarning("[GameUIManager] AssistantManager.Instance is null");
            }
            
            Debug.Log("[GameUIManager] Triggering manual sync...");
            SyncAllPlayerSlots();
        }
        
        // 게임 시작 테스트 키 (G키 - 서버에서만)
        if (Input.GetKeyDown(KeyCode.G))
        {
            var runner = FindObjectOfType<NetworkRunner>();
            if (runner != null && runner.IsServer && isAssistantReady)
            {
                Debug.Log("[GameUIManager] Manual game start triggered (G key)");
                RequestGameStart();
            }
            else
            {
                Debug.Log("[GameUIManager] G key pressed but conditions not met");
            }
        }
        
        // Assistant 상태 강제 체크 (A키)
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (AssistantManager.Instance != null)
            {
                AssistantStatus status = AssistantManager.Instance.GetCurrentStatus();
                bool round1Ready = AssistantManager.Instance.IsRoundEventsReady(1);
                string message = AssistantManager.Instance.GetStatusMessage();
                Debug.Log($"[GameUIManager] Manual Assistant check - Status: {status}, Message: '{message}', Round 1 Ready: {round1Ready}");
                
                if (status == AssistantStatus.Ready && round1Ready && !isAssistantReady)
                {
                    UpdateAssistantStatus("준비 완료!", true);
                    isAssistantReady = true;
                    ToggleStartButton();
                }
            }
            else
            {
                Debug.LogWarning("[GameUIManager] AssistantManager.Instance is null");
            }
        }
    }
    #endregion
}